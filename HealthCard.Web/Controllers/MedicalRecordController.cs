using Data;
using HealthCard.Services.Interfaces;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.ParameterBase;

namespace HealthCard.Web.Controllers;

public class MedicalRecordController(ApplicationDbContext db, IFileStorageService files, RecordQueryService queries, ILogger<MedicalRecordController> logger) : UserController(db)
{
    [HttpGet]
    public async Task<IActionResult> Create(int? categoryId)
    {
        var model = new RecordCreateViewModel { CategoryId = categoryId ?? 0 };
        if (categoryId.HasValue && !await queries.Categories(UserId).AnyAsync(c => c.Id == categoryId)) return NotFound();
        await Populate(model);
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Parameters(int categoryId)
    {
        var category = await queries.Categories(UserId).FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category == null) return NotFound();
        var model = new RecordCreateViewModel { CategoryId = categoryId };
        await Populate(model);
        return PartialView("_Parameters", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(RecordCreateViewModel model) => Save(model, false);
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await Db.MedicalRecords.Include(r => r.TestResults).FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record == null) return NotFound();
        var model = new RecordCreateViewModel
        {
            Id = id,
            CategoryId = record.CategoryId,
            RecordDate = record.RecordDate,
            Title = record.Title,
            Comment = record.Comment,
            ExistingFilePath = record.FilePath,
            Parameters = record.TestResults.Select(r => new ParameterInputViewModel { TestParameterId = r.TestParameterId, Value = r.Value }).ToList()
        };
        await Populate(model);
        return View("Create", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, RecordCreateViewModel model)
    {
        if (id != model.Id) return Task.FromResult<IActionResult>(BadRequest());
        return Save(model, true);
    }
    private async Task Populate(RecordCreateViewModel model)
    {
        model.Categories = await queries.Categories(UserId).OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategorySelectViewModel { Id = c.Id, Name = c.Name, IsLabTest = c.IsLabTest }).ToListAsync();
        model.IsLabTest = model.Categories.Any(c => c.Id == model.CategoryId && c.IsLabTest);
        var values = model.Parameters.GroupBy(p => p.TestParameterId).ToDictionary(g => g.Key, g => g.First().Value);
        var norms = await Db.UserParameterNorms.AsNoTracking().Where(n => n.UserId == UserId).ToDictionaryAsync(n => n.TestParameterId);
        model.Parameters = model.IsLabTest ? await Db.TestParameters.AsNoTracking().Where(p => p.CategoryId == model.CategoryId).OrderBy(p => p.SortOrder)
            .Select(p => new ParameterInputViewModel { TestParameterId = p.Id, ParameterName = p.Name, Unit = p.Unit, NormMin = p.NormMin, NormMax = p.NormMax }).ToListAsync() : new();
        foreach (var parameter in model.Parameters)
        {
            parameter.Value = values.GetValueOrDefault(parameter.TestParameterId);
            if (norms.TryGetValue(parameter.TestParameterId, out var norm)) { parameter.NormMin = norm.CustomNormMin; parameter.NormMax = norm.CustomNormMax; }
        }
    }
    private async Task<IActionResult> Save(RecordCreateViewModel model, bool editing)
    {
        if (!editing) model.Id = 0;
        MedicalRecord? record = null;
        if (editing)
        {
            record = await Db.MedicalRecords.Include(r => r.TestResults).FirstOrDefaultAsync(r => r.Id == model.Id && r.UserId == UserId);
            if (record == null) return NotFound();
            model.ExistingFilePath = record.FilePath;
            if (record.CategoryId != model.CategoryId) ModelState.AddModelError(nameof(model.CategoryId), "Категорию сохраненной записи менять нельзя. Создайте новую запись в нужной категории.");
        }
        var category = await queries.Categories(UserId).FirstOrDefaultAsync(c => c.Id == model.CategoryId);
        if (category == null) ModelState.AddModelError(nameof(model.CategoryId), "Категория недоступна");
        var allowedIds = await Db.TestParameters.Where(p => p.CategoryId == model.CategoryId).Select(p => p.Id).ToListAsync();
        if (model.Parameters.Select(p => p.TestParameterId).Distinct().Count() != model.Parameters.Count || model.Parameters.Any(p => !allowedIds.Contains(p.TestParameterId)) || (category is { IsLabTest: false } && model.Parameters.Count > 0))
            ModelState.AddModelError("", "Состав показателей изменился. Выберите категорию заново и проверьте значения.");
        if (model.File is { } file)
        {
            if (file.Length == 0) ModelState.AddModelError("File", "Выбранный файл пуст");
            else if (file.Length > 10 * 1024 * 1024) ModelState.AddModelError("File", "Размер файла не должен превышать 10 МБ");
            else if (!await ValidFile(file)) ModelState.AddModelError("File", "Загрузите настоящий PDF, JPG или PNG");
        }
        if (!ModelState.IsValid) { await Populate(model); return View("Create", model); }
        string? newPath = null;
        var oldPath = record?.FilePath;
        try
        {
            if (model.File is { Length: > 0 })
            {
                await using var stream = model.File.OpenReadStream();
                newPath = await files.SaveFileAsync(stream, model.File.FileName, UserId);
            }
            if (record == null)
            {
                record = MedicalRecord.Create(UserId, model.CategoryId, model.RecordDate, model.Title, model.Comment);
                Db.MedicalRecords.Add(record);
            }
            else { record.Update(model.RecordDate, model.Title, model.Comment); Db.TestResults.RemoveRange(record.TestResults); record.TestResults.Clear(); }
            if (newPath != null || model.RemoveFile) record.SetFilePath(newPath);
            foreach (var parameter in model.Parameters.Where(p => p.Value.HasValue))
                record.AddResult(TestResult.Create(record.Id, parameter.TestParameterId, parameter.Value!.Value));
            // One SaveChanges stores the complete graph in a single database transaction.
            await Db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or IOException or ArgumentException)
        {
            if (newPath != null) await TryDelete(newPath);
            logger.LogError(ex, "Не удалось сохранить медицинскую запись");
            Db.ChangeTracker.Clear();
            ModelState.AddModelError("", "Не удалось сохранить запись. Проверьте данные и повторите попытку.");
            await Populate(model);
            return View("Create", model);
        }
        if (oldPath != null && oldPath != record.FilePath) await TryDelete(oldPath);
        TempData["SuccessMessage"] = editing ? "Запись обновлена" : "Запись добавлена";
        return RedirectToAction(nameof(Details), new { id = record.Id });
    }
    private async Task TryDelete(string path)
    {
        try { await files.DeleteFileAsync(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { logger.LogWarning(ex, "Не удалось удалить неиспользуемое вложение"); }
    }
    private static async Task<bool> ValidFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var count = await stream.ReadAtLeastAsync(header, 8, throwOnEndOfStream: false);
        return extension switch
        {
            ".pdf" => count >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".png" => count == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => count >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255,
            _ => false
        };
    }
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, DateOnly? fromDate, DateOnly? toDate, int page = 1)
    {
        if (fromDate > toDate) ModelState.AddModelError("", "Начальная дата должна быть не позже конечной");
        var query = Db.MedicalRecords.AsNoTracking().Include(r => r.Category).Where(r => r.UserId == UserId);
        if (categoryId.HasValue) query = query.Where(r => r.CategoryId == categoryId);
        if (fromDate.HasValue) query = query.Where(r => r.RecordDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(r => r.RecordDate <= toDate.Value);
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / 12d)); page = Math.Clamp(page, 1, pages);
        return View(new RecordListViewModel
        {
            CategoryId = categoryId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            TotalCount = count,
            TotalPages = pages,
            Categories = await queries.Categories(UserId).OrderBy(c => c.SortOrder).ToListAsync(),
            Records = await query.OrderByDescending(r => r.RecordDate).ThenByDescending(r => r.Id).Skip((page - 1) * 12).Take(12).ToListAsync()
        });
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var record = await Db.MedicalRecords.AsNoTracking().Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record == null) return NotFound();
        return View(new RecordDetailsViewModel { Record = record, Results = await queries.ResultsAsync(UserId, recordId: id) });
    }
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var record = await Db.MedicalRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record?.FilePath == null) return NotFound();
        try
        {
            var extension = Path.GetExtension(record.FilePath).ToLowerInvariant();
            var type = extension switch { ".pdf" => "application/pdf", ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", _ => "application/octet-stream" };
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.CacheControl = "private, no-store";
            return File(await files.GetFileBytesAsync(record.FilePath), type, $"record-{record.Id}{extension}");
        }
        catch (FileNotFoundException) { return NotFound("Вложение не найдено на диске"); }
        catch (DirectoryNotFoundException) { return NotFound("Вложение не найдено на диске"); }
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await Db.MedicalRecords.Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        return record == null ? NotFound() : View(record);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var record = await Db.MedicalRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record == null) return NotFound();
        var path = record.FilePath;
        Db.MedicalRecords.Remove(record); await Db.SaveChangesAsync();
        if (path != null) await TryDelete(path);
        TempData["SuccessMessage"] = "Запись удалена";
        return RedirectToAction(nameof(Index));
    }
}
