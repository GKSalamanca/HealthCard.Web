using Data;
using HealthCard.Web.ViewModels;
using ViewModels.Entrance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.ParameterBase;
using Models.UserSettings;

namespace HealthCard.Web.Controllers;

[Authorize]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CategoryController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Categories/GroupDetails?type=LabTest или type=Research
    public async Task<IActionResult> GroupDetails(string? type)
    {
        var userId = _userManager.GetUserId(User);

        // ✅ ИСПРАВЛЕНИЕ CS8604: проверяем userId на null
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Определяем тип
        bool? isLabTest = null;
        string title;

        if (type == "LabTest")
        {
            isLabTest = true;
            title = "Лабораторные анализы";
        }
        else if (type == "Research")
        {
            isLabTest = false;
            title = "Исследования и заключения";
        }
        else
        {
            title = "Все категории";
        }

        // ✅ ИСПРАВЛЕНИЕ CS0266: сначала все Where, потом OrderBy в конце
        IQueryable<Category> categoriesQuery = _context.Categories
            .Where(c => c.IsSystem);

        if (isLabTest.HasValue)
        {
            categoriesQuery = categoriesQuery.Where(c => c.IsLabTest == isLabTest.Value);
        }

        // OrderBy применяем ТОЛЬКО в конце, перед ToListAsync
        var categories = await categoriesQuery
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        // Для каждой категории считаем количество записей пользователя
        var categoryStats = new List<CategoryStatsViewModel>();
        foreach (var category in categories)
        {
            var recordsCount = await _context.MedicalRecords
                .CountAsync(mr => mr.UserId == userId && mr.CategoryId == category.Id);

            var lastRecord = await _context.MedicalRecords
                .Where(mr => mr.UserId == userId && mr.CategoryId == category.Id)
                .OrderByDescending(mr => mr.RecordDate)
                .FirstOrDefaultAsync();

            categoryStats.Add(new CategoryStatsViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Description = category.Description,
                IconClass = category.IconClass,
                RecordsCount = recordsCount,
                LastRecordDate = lastRecord?.RecordDate
            });
        }

        ViewBag.IsLabTest = isLabTest;
        ViewBag.Title = title;
        return View(categoryStats);
    }
}