using Data;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.UserSettings;
namespace HealthCard.Web.Controllers;

public class NormsController(ApplicationDbContext db, RecordQueryService queries) : UserController(db)
{
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId)
    {
        var categories = await queries.Categories(UserId).Where(c => c.IsLabTest).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        if (categoryId.HasValue && !categories.Any(c => c.Id == categoryId)) return NotFound();
        var ids = categories.Select(c => c.Id).ToList();
        var parameters = await Db.TestParameters.AsNoTracking().Include(p => p.Category).Where(p => ids.Contains(p.CategoryId) && (!categoryId.HasValue || p.CategoryId == categoryId)).OrderBy(p => p.Category.SortOrder).ThenBy(p => p.SortOrder).ToListAsync();
        var norms = await Db.UserParameterNorms.AsNoTracking().Where(n => n.UserId == UserId).ToDictionaryAsync(n => n.TestParameterId);
        return View(new NormListViewModel { CategoryId = categoryId, Categories = categories, Parameters = parameters.Select(p => new NormListRow(p.Id, p.Name, p.Unit, p.Category.Name, p.NormMin, p.NormMax, norms.GetValueOrDefault(p.Id)?.CustomNormMin, norms.GetValueOrDefault(p.Id)?.CustomNormMax)).ToList() });
    }
    private Task<global::Models.ParameterBase.TestParameter?> Parameter(int id) => Db.TestParameters.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id && (p.Category.IsSystem || p.Category.UserId == UserId));
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await Parameter(id); if (p == null) return NotFound();
        var n = await Db.UserParameterNorms.FirstOrDefaultAsync(n => n.UserId == UserId && n.TestParameterId == id);
        return View(new NormEditViewModel { TestParameterId = id, Name = p.Name, Unit = p.Unit, DefaultMin = p.NormMin, DefaultMax = p.NormMax, NormMin = n?.CustomNormMin ?? p.NormMin, NormMax = n?.CustomNormMax ?? p.NormMax });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NormEditViewModel model)
    {
        if (id != model.TestParameterId) return BadRequest();
        var p = await Parameter(id); if (p == null) return NotFound();
        model.Name = p.Name; model.Unit = p.Unit; model.DefaultMin = p.NormMin; model.DefaultMax = p.NormMax;
        if (!ModelState.IsValid) return View(model);
        var n = await Db.UserParameterNorms.FirstOrDefaultAsync(n => n.UserId == UserId && n.TestParameterId == id);
        if (n == null) Db.UserParameterNorms.Add(UserParameterNorm.Create(UserId, id, model.NormMin, model.NormMax)); else n.UpdateNorms(model.NormMin, model.NormMax);
        await Db.SaveChangesAsync(); TempData["SuccessMessage"] = "Персональный интервал сохранен и применяется на всех страницах";
        return RedirectToAction(nameof(Index), new { categoryId = p.CategoryId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(int id)
    {
        var p = await Parameter(id); if (p == null) return NotFound();
        var n = await Db.UserParameterNorms.FirstOrDefaultAsync(n => n.UserId == UserId && n.TestParameterId == id);
        if (n != null) { Db.UserParameterNorms.Remove(n); await Db.SaveChangesAsync(); }
        TempData["SuccessMessage"] = "Восстановлен общий интервал";
        return RedirectToAction(nameof(Index), new { categoryId = p.CategoryId });
    }
}
