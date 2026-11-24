using Data;
using Models.ParameterBase;
using Models.UserSettings;
using HealthCard.Services.Interfaces;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCard.Web.Controllers;

[Authorize]
public class MedicalRecordController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileService;

    public MedicalRecordController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileService)
    {
        _context = context;
        _userManager = userManager;
        _fileService = fileService;
    }

    // GET: /MedicalRecord/Create
    [HttpGet]
    public async Task<IActionResult> Create(int? categoryId)
    {
        var userId = _userManager.GetUserId(User);

        var model = new RecordCreateViewModel
        {
            RecordDate = DateOnly.FromDateTime(DateTime.Today),
            CategoryId = categoryId ?? 0,
            Categories = await _context.Categories
           .Where(c => c.IsSystem || c.UserId == userId)
           .OrderBy(c => c.SortOrder)
           .Select(c => new CategorySelectViewModel
           {
               Id = c.Id,
               Name = c.Name,
               IsLabTest = c.IsLabTest
           })
           .ToListAsync()
        };

        // Если категория выбрана, загружаем её параметры
        if (categoryId.HasValue && categoryId > 0)
        {
            var category = await _context.Categories.FindAsync(categoryId.Value);
            if (category != null && category.IsLabTest)
            {
                model.IsLabTest = true;
                model.Parameters = await _context.TestParameters
                    .Where(p => p.CategoryId == categoryId.Value)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new ParameterInputViewModel
                    {
                        TestParameterId = p.Id,
                        ParameterName = p.Name,
                        Unit = p.Unit,
                        NormMin = p.NormMin,
                        NormMax = p.NormMax,
                        Value = null // Поле для ввода
                    })
                    .ToListAsync();
            }
        }

        return View(model);
    }

    // POST: /MedicalRecord/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecordCreateViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        // Загружаем категории для выпадающего списка
        model.Categories = await _context.Categories
            .Where(c => c.IsSystem || c.UserId == userId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategorySelectViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IsLabTest = c.IsLabTest
            })
            .ToListAsync();

        if (ModelState.IsValid)
        {
            // Получаем категорию
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Категория не найдена");
                return View(model);
            }

            // Создаем запись через factory method
            var record = MedicalRecord.Create(
                userId,
                model.CategoryId,
                model.RecordDate,
                model.Title,
                model.Comment
            );

            // Сохраняем файл, если он есть
            if (model.File != null && model.File.Length > 0)
            {
                // Проверяем размер (10 МБ)
                if (model.File.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("File", "Размер файла не должен превышать 10 МБ");
                    return View(model);
                }

                // Проверяем расширение
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(model.File.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("File", "Допустимые форматы: PDF, JPG, PNG");
                    return View(model);
                }

                var filePath = await _fileService.SaveFileAsync(
                    model.File.OpenReadStream(),
                    model.File.FileName,
                    userId
                );
                record.SetFilePath(filePath);
            }

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();

            // Если это лабораторный анализ, сохраняем результаты
            if (category.IsLabTest && model.Parameters != null)
            {
                foreach (var param in model.Parameters)
                {
                    if (param.Value.HasValue)
                    {
                        var result = TestResult.Create(
                            record.Id,
                            param.TestParameterId,
                            param.Value.Value
                        );
                        _context.TestResults.Add(result);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Запись успешно добавлена!";
            return RedirectToAction("Index");
        }

        return View(model);
    }

    // GET: /MedicalRecord/Index
    public async Task<IActionResult> Index(int? categoryId, DateTime? fromDate, DateTime? toDate)
    {
        var userId = _userManager.GetUserId(User);

        var query = _context.MedicalRecords
            .Include(mr => mr.Category)
            .Where(mr => mr.UserId == userId);

        // Фильтрация по категории
        if (categoryId.HasValue)
        {
            query = query.Where(mr => mr.CategoryId == categoryId.Value);
        }

        // Фильтрация по дате
        if (fromDate.HasValue)
        {
            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(mr => mr.RecordDate >= fromDateOnly);
        }

        if (toDate.HasValue)
        {
            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(mr => mr.RecordDate <= toDateOnly);
        }

        var records = await query
            .OrderByDescending(mr => mr.RecordDate)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories
            .Where(c => c.IsSystem || c.UserId == userId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return View(records);
    }

    // GET: /MedicalRecord/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);

        var record = await _context.MedicalRecords
            .Include(mr => mr.Category)
            .Include(mr => mr.TestResults)
                .ThenInclude(tr => tr.TestParameter)
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.UserId == userId);

        if (record == null)
            return NotFound();

        // Получаем персональные нормы для пользователя
        var userNorms = await _context.UserParameterNorms
            .Where(upn => upn.UserId == userId)
            .ToListAsync();

        var results = record.TestResults.Select(tr =>
        {
            // Проверяем, есть ли персональная норма
            var userNorm = userNorms.FirstOrDefault(un => un.TestParameterId == tr.TestParameterId);

            var normMin = userNorm?.CustomNormMin ?? tr.TestParameter.NormMin;
            var normMax = userNorm?.CustomNormMax ?? tr.TestParameter.NormMax;

            return new
            {
                tr.Id,
                ParameterName = tr.TestParameter.Name,
                Unit = tr.TestParameter.Unit,
                tr.Value,
                NormMin = normMin,
                NormMax = normMax,
                IsOutOfRange = tr.Value < normMin || tr.Value > normMax,
                IsNearOutOfRange = tr.Value >= normMin && tr.Value <= normMax &&
                    ((tr.Value <= normMin * 1.1m && tr.Value >= normMin) ||
                     (tr.Value >= normMax * 0.9m && tr.Value <= normMax))
            };
        }).ToList();

        ViewBag.Results = results;
        ViewBag.Record = record;

        return View();
    }

    // GET: /MedicalRecord/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);

        var record = await _context.MedicalRecords
            .Include(mr => mr.Category)
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.UserId == userId);

        if (record == null)
            return NotFound();

        return View(record);
    }

    // POST: /MedicalRecord/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);

        var record = await _context.MedicalRecords
            .Include(mr => mr.TestResults)
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.UserId == userId);

        if (record != null)
        {
            // Удаляем файл
            if (!string.IsNullOrEmpty(record.FilePath))
            {
                await _fileService.DeleteFileAsync(record.FilePath);
            }

            _context.MedicalRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Запись удалена!";
        }

        return RedirectToAction("Index");
    }
}