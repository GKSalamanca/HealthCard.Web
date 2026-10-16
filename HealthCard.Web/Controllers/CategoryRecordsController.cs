using Data;
using Models.UserSettings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCard.Web.Controllers;

[Authorize]
public class CategoryRecordsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CategoryRecordsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /CategoryRecords/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        // Получаем все записи пользователя по этой категории
        var records = await _context.MedicalRecords
            .Include(mr => mr.TestResults)
                .ThenInclude(tr => tr.TestParameter)
            .Where(mr => mr.UserId == userId && mr.CategoryId == id)
            .OrderByDescending(mr => mr.RecordDate)
            .ToListAsync();

        if (!category.IsLabTest || !records.Any())
        {
            ViewBag.Category = category;
            ViewBag.Records = records;
            return View(records);
        }

        // Для лабораторных анализов формируем матрицу: строки - параметры, колонки - даты
        var allParameters = await _context.TestParameters
            .Where(p => p.CategoryId == id)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var dates = records.Select(r => r.RecordDate).Distinct().OrderBy(d => d).ToList();

        var matrix = new List<ParameterRowViewModel>();
        foreach (var param in allParameters)
        {
            var row = new ParameterRowViewModel
            {
                ParameterName = param.Name,
                Unit = param.Unit,
                NormMin = param.NormMin,
                NormMax = param.NormMax,
                Values = new Dictionary<DateOnly, decimal?>()
            };

            foreach (var record in records)
            {
                var result = record.TestResults.FirstOrDefault(tr => tr.TestParameterId == param.Id);
                row.Values[record.RecordDate] = result?.Value;
            }

            matrix.Add(row);
        }

        ViewBag.Category = category;
        ViewBag.Dates = dates;
        ViewBag.Matrix = matrix;

        return View(records);
    }
}

public class ParameterRowViewModel
{
    public string ParameterName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal NormMin { get; set; }
    public decimal NormMax { get; set; }
    public Dictionary<DateOnly, decimal?> Values { get; set; } = new();
}