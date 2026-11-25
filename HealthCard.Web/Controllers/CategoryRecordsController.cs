using Data;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HealthCard.Web.Controllers;

public class CategoryRecordsController(ApplicationDbContext db, RecordQueryService queries) : UserController(db)
{
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var category = await queries.Categories(UserId).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        var results = await queries.ResultsAsync(UserId, id);
        return View(new CategoryRecordsViewModel
        {
            Category = category,
            Records = await Db.MedicalRecords.AsNoTracking().Where(r => r.UserId == UserId && r.CategoryId == id).OrderBy(r => r.RecordDate).ThenBy(r => r.Id).ToListAsync(),
            Parameters = await Db.TestParameters.AsNoTracking().Where(p => p.CategoryId == id).OrderBy(p => p.SortOrder).ToListAsync(),
            Results = results.GroupBy(r => (r.RecordId, r.ParameterId)).ToDictionary(g => g.Key, g => g.Last())
        });
    }
}
