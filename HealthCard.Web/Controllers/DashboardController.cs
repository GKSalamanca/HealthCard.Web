using Data;
using Models.UserSettings;
using Services.Interfaces;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace HealthCard.Web.Controllers;

[Authorize] // Требует авторизации
public class DashboardController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public DashboardController(
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _categoryService = categoryService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);

        var categories = (await _categoryService.GetCategoriesAsync()).ToList();

        var recentRecords = await _context.MedicalRecords
            .Include(mr => mr.Category)
            .Where(mr => mr.UserId == userId)
            .OrderByDescending(mr => mr.RecordDate)
            .Take(5)
            .ToListAsync();

        //Статистика по категориям
        var categoryStats = new List<CategoryStatisticsViewModel>();
        foreach (var category in categories)
        {
            var recordsCount = await _context.MedicalRecords
                .CountAsync(mr => mr.UserId == userId && mr.CategoryId == category.Id);

            var lastRecord = await _context.MedicalRecords
                .Where(mr => mr.UserId == userId && mr.CategoryId == category.Id)
                .OrderByDescending(mr => mr.RecordDate)
                .FirstOrDefaultAsync();

            categoryStats.Add(new CategoryStatisticsViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Description = category.Description,
                IsLabTest = category.IsLabTest,
                IconClass = category.IconClass,
                RecordsCount = recordsCount,
                LastRecordDate = lastRecord?.RecordDate
            });
        }

        var model = new DashboardViewModel
        {
            UserName = user?.FullName ?? "Пользователь",
            TotalRecordsCount = await _context.MedicalRecords.CountAsync(mr => mr.UserId == userId),
            RecentRecords = recentRecords,
            Categories = categories,
            CategoryStatistics = categoryStats
        };

        return View(model);
    }
}