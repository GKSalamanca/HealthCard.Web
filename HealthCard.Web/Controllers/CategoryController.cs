using Data;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.ParameterBase;
namespace HealthCard.Web.Controllers;

public class CategoryController(ApplicationDbContext db, RecordQueryService queries) : UserController(db)
{
    [HttpGet] public Task<IActionResult> Index() => GroupDetails(null);
    [HttpGet]
    public async Task<IActionResult> GroupDetails(string? type)
    {
        var query = queries.Categories(UserId);
        if (type == "LabTest") query = query.Where(c => c.IsLabTest);
        if (type == "Research") query = query.Where(c => !c.IsLabTest);
        var stats = await Db.MedicalRecords.Where(r => r.UserId == UserId).GroupBy(r => r.CategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count(), Date = g.Max(r => r.RecordDate) }).ToDictionaryAsync(g => g.Id);
        var categories = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        var model = categories.Select(c => new CategoryStatsViewModel
        {
            CategoryId = c.Id,
            CategoryName = c.Name,
            Description = c.Description,
            IconClass = c.IconClass,
            IsSystem = c.IsSystem,
            IsLabTest = c.IsLabTest,
            RecordsCount = stats.TryGetValue(c.Id, out var s) ? s.Count : 0,
            LastRecordDate = stats.TryGetValue(c.Id, out var t) ? t.Date : null
        }).ToList();
        ViewBag.Title = type == "LabTest" ? "Лабораторные анализы" : type == "Research" ? "Исследования и заключения" : "Категории";
        return View("GroupDetails", model);
    }
    [HttpGet] public IActionResult Create() => View("Edit", new CategoryEditViewModel { Parameters = new() { new() } });
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Create(CategoryEditViewModel model) => Save(model, false);
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await Db.Categories.Include(c => c.TestParameters).FirstOrDefaultAsync(c => c.Id == id && !c.IsSystem && c.UserId == UserId);
        if (category == null) return NotFound();
        return View(new CategoryEditViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsLabTest = category.IsLabTest,
            HasRecords = await Db.MedicalRecords.AnyAsync(r => r.CategoryId == id),
            Parameters = category.TestParameters.OrderBy(p => p.SortOrder).Select(p => new CategoryParameterViewModel { Id = p.Id, Name = p.Name, Unit = p.Unit, NormMin = p.NormMin, NormMax = p.NormMax }).ToList()
        });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, CategoryEditViewModel model) => id != model.Id ? Task.FromResult<IActionResult>(BadRequest()) : Save(model, true);
    private async Task<IActionResult> Save(CategoryEditViewModel model, bool editing)
    {
        if (!editing) model.Id = 0;
        Category? category = null;
        if (editing)
        {
            category = await Db.Categories.Include(c => c.TestParameters).FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsSystem && c.UserId == UserId);
            if (category == null) return NotFound();
            if (model.IsLabTest != category.IsLabTest) ModelState.AddModelError("", "Тип существующей категории изменить нельзя");
            model.HasRecords = await Db.MedicalRecords.AnyAsync(r => r.CategoryId == category.Id);
        }
        // Removed rows and document-only categories do not require parameter input.
        for (var i = 0; i < model.Parameters.Count; i++)
            if (model.Parameters[i].Remove || !model.IsLabTest)
                foreach (var key in ModelState.Keys.Where(k => k.StartsWith($"Parameters[{i}].", StringComparison.Ordinal)).ToList()) ModelState.Remove(key);
        if (model.Parameters.Count > 100) ModelState.AddModelError("", "В категории допускается не более 100 показателей");
        if (await Db.Categories.AnyAsync(c => (c.IsSystem || c.UserId == UserId) && c.Id != model.Id && c.Name.ToLower() == (model.Name ?? "").Trim().ToLower()))
            ModelState.AddModelError(nameof(model.Name), "Категория с таким названием уже существует");
        var existing = category?.TestParameters.ToDictionary(p => p.Id) ?? new();
        if (model.Parameters.Where(p => p.Id != 0).GroupBy(p => p.Id).Any(g => g.Count() > 1) || model.Parameters.Any(p => p.Id != 0 && !existing.ContainsKey(p.Id)))
            ModelState.AddModelError("", "Некорректный состав показателей категории");
        var removedIds = existing.Keys.Except(model.Parameters.Where(p => !p.Remove).Select(p => p.Id)).ToList();
        if (await Db.TestResults.AnyAsync(r => removedIds.Contains(r.TestParameterId)))
            ModelState.AddModelError("", "Нельзя удалить показатель, для которого сохранены результаты. Снимите отметку удаления.");
        if (!ModelState.IsValid) return View("Edit", model);
        if (category == null)
        {
            category = Category.CreateUser(UserId, model.Name, model.IsLabTest, 1000, model.Description, model.IsLabTest ? "bi bi-droplet" : "bi bi-folder2-open");
            Db.Categories.Add(category);
        }
        else category.Update(model.Name, model.Description, category.IconClass);
        Db.TestParameters.RemoveRange(existing.Values.Where(p => removedIds.Contains(p.Id)));
        if (model.IsLabTest)
        {
            var order = 0;
            foreach (var input in model.Parameters.Where(p => !p.Remove))
            {
                order++;
                if (existing.TryGetValue(input.Id, out var parameter)) { parameter.Update(input.Name, input.Unit); parameter.UpdateNorms(input.NormMin, input.NormMax); parameter.SetSortOrder(order); }
                else category.AddParameter(TestParameter.Create(category.Id, input.Name, input.Unit, input.NormMin, input.NormMax, order, false));
            }
        }
        await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = editing ? "Категория обновлена" : "Ваша категория создана";
        return RedirectToAction("Details", "CategoryRecords", new { id = category.Id });
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsSystem && c.UserId == UserId);
        if (category == null) return NotFound();
        ViewBag.RecordsCount = await Db.MedicalRecords.CountAsync(r => r.CategoryId == id);
        return View(category);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsSystem && c.UserId == UserId);
        if (category == null) return NotFound();
        if (await Db.MedicalRecords.AnyAsync(r => r.CategoryId == id))
        { TempData["ErrorMessage"] = "В категории есть записи. Сначала удалите их или сохраните категорию для истории."; return RedirectToAction(nameof(Delete), new { id }); }
        Db.Categories.Remove(category); await Db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Категория удалена";
        return RedirectToAction(nameof(Index));
    }
}
