using Data;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.ParameterBase;
namespace HealthCard.Web.Controllers;

public class DashboardController(ApplicationDbContext db, RecordQueryService queries) : UserController(db)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await queries.Categories(UserId).OrderBy(c => c.SortOrder).ToListAsync();
        var stats = await Db.MedicalRecords.Where(r => r.UserId == UserId).GroupBy(r => r.CategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count(), Date = g.Max(r => r.RecordDate) }).ToDictionaryAsync(g => g.Id);
        var user = await Db.Users.AsNoTracking().FirstAsync(u => u.Id == UserId);
        return View(new DashboardViewModel
        {
            UserName = user.FullName,
            TotalRecordsCount = stats.Values.Sum(s => s.Count),
            Categories = categories,
            OutOfRangeCount = (await queries.ResultsAsync(UserId)).Count(r => r.Status == ResultStatus.OutOfRange),
            RecentRecords = await Db.MedicalRecords.AsNoTracking().Include(r => r.Category).Where(r => r.UserId == UserId).OrderByDescending(r => r.RecordDate).ThenByDescending(r => r.Id).Take(5).ToListAsync(),
            CategoryStatistics = categories.Select(c => new CategoryStatisticsViewModel
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                Description = c.Description,
                IconClass = c.IconClass,
                IsLabTest = c.IsLabTest,
                RecordsCount = stats.TryGetValue(c.Id, out var s) ? s.Count : 0,
                LastRecordDate = stats.TryGetValue(c.Id, out var t) ? t.Date : null
            }).ToList()
        });
    }
}
