using System.Globalization;
using System.Text;
using Data;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
namespace HealthCard.Web.Controllers;

public class AnalyticsController(ApplicationDbContext db, RecordQueryService queries) : UserController(db)
{
    private async Task<AnalyticsViewModel> Build(int? categoryId, int? parameterId, DateOnly? fromDate, DateOnly? toDate)
    {
        var categories = await queries.Categories(UserId).Where(c => c.IsLabTest).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        if (categoryId.HasValue && !categories.Any(c => c.Id == categoryId)) ModelState.AddModelError("", "Категория недоступна");
        categoryId ??= await Db.MedicalRecords.Where(r => r.UserId == UserId && r.Category.IsLabTest).OrderByDescending(r => r.RecordDate).Select(r => (int?)r.CategoryId).FirstOrDefaultAsync() ?? categories.FirstOrDefault()?.Id;
        if (fromDate > toDate) ModelState.AddModelError("", "Начальная дата должна быть не позже конечной");
        var visibleIds = categories.Select(c => c.Id).ToList();
        var parameters = await Db.TestParameters.AsNoTracking().Where(p => p.CategoryId == categoryId && visibleIds.Contains(p.CategoryId)).OrderBy(p => p.SortOrder).ToListAsync();
        if (parameterId.HasValue && !parameters.Any(p => p.Id == parameterId)) ModelState.AddModelError("", "Показатель не относится к выбранной категории");
        var all = ModelState.IsValid && categoryId.HasValue ? await queries.ResultsAsync(UserId, categoryId, fromDate, toDate) : new List<ResultRowViewModel>();
        parameterId ??= all.FirstOrDefault()?.ParameterId ?? parameters.FirstOrDefault()?.Id;
        var series = all.Where(r => r.ParameterId == parameterId).ToList();
        var countQuery = Db.MedicalRecords.Where(r => r.UserId == UserId && r.CategoryId == categoryId);
        if (fromDate.HasValue) countQuery = countQuery.Where(r => r.RecordDate >= fromDate.Value);
        if (toDate.HasValue) countQuery = countQuery.Where(r => r.RecordDate <= toDate.Value);
        return new AnalyticsViewModel
        {
            CategoryId = categoryId,
            ParameterId = parameterId,
            FromDate = fromDate,
            ToDate = toDate,
            Categories = categories,
            Parameters = parameters,
            Results = series,
            Series = series,
            TotalRecords = ModelState.IsValid ? await countQuery.CountAsync() : 0
        };
    }
    [HttpGet] public async Task<IActionResult> Index(int? categoryId, int? parameterId, DateOnly? fromDate, DateOnly? toDate) => View(await Build(categoryId, parameterId, fromDate, toDate));
    [HttpGet]
    public async Task<IActionResult> Export(int? categoryId, int? parameterId, DateOnly? fromDate, DateOnly? toDate, string format = "csv")
    {
        var model = await Build(categoryId, parameterId, fromDate, toDate);
        if (!ModelState.IsValid) return BadRequest("Проверьте фильтры отчета");
        if (format == "csv")
        {
            static string Cell(string text) { if (text.Length > 0 && "=+-@\t\r".Contains(text[0])) text = "'" + text; return "\"" + text.Replace("\"", "\"\"") + "\""; }
            var csv = new StringBuilder("Дата;Категория;Показатель;Результат;Единица;Нижняя граница;Верхняя граница;Статус\r\n");
            foreach (var r in model.Results) csv.AppendLine(string.Join(";", new[] { r.Date.ToString("yyyy-MM-dd"), r.CategoryName, r.ParameterName, r.Value.ToString(CultureInfo.InvariantCulture), r.Unit, r.NormMin.ToString(CultureInfo.InvariantCulture), r.NormMax.ToString(CultureInfo.InvariantCulture), r.StatusText }.Select(Cell)));
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "healthcard-analysis.csv");
        }
        if (format != "pdf") return BadRequest("Неизвестный формат отчета");
        var pdf = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(25); page.DefaultTextStyle(s => s.FontSize(9));
            page.Header().Column(c => { c.Item().Text("Карточка здоровья — результаты анализов").FontSize(18).SemiBold(); c.Item().Text($"Период: {fromDate?.ToString("dd.MM.yyyy") ?? "начало истории"} — {toDate?.ToString("dd.MM.yyyy") ?? "сегодня"}. Измерений: {model.Results.Count}"); });
            page.Content().PaddingVertical(12).Column(c =>
            {
                if (model.Results.Count == 0) c.Item().Text("По выбранным фильтрам результатов нет.");
                else c.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.ConstantColumn(60); d.RelativeColumn(3); d.RelativeColumn(); d.RelativeColumn(); d.RelativeColumn(2); d.RelativeColumn(2); });
                    t.Header(h => { foreach (var label in new[] { "Дата", "Показатель", "Результат", "Единица", "Интервал", "Статус" }) h.Cell().Background("#E8EDF5").Padding(6).Text(label).SemiBold(); });
                    foreach (var r in model.Results) foreach (var value in new[] { r.Date.ToString("dd.MM.yyyy"), r.ParameterName, r.Value.ToString("0.####"), r.Unit, $"{r.NormMin:0.####} — {r.NormMax:0.####}", r.StatusText }) t.Cell().BorderBottom(.5f).BorderColor("#D9DDE5").Padding(6).Text(value);
                });
                c.Item().PaddingTop(12).Text("Цветовые статусы отражают сравнение с заданным интервалом. Медицинское заключение не формируется.").FontSize(8);
            });
            page.Footer().AlignCenter().Text(t => { t.Span("Страница "); t.CurrentPageNumber(); });
        })).GeneratePdf();
        return File(pdf, "application/pdf", "healthcard-analysis.pdf");
    }
}
