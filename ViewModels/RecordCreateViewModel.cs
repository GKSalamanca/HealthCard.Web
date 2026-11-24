using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HealthCard.Web.ViewModels;

public class RecordCreateViewModel
{
    [Required(ErrorMessage = "Выберите категорию")]
    [Display(Name = "Категория")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Введите дату")]
    [Display(Name = "Дата сдачи анализа")]
    [DataType(DataType.Date)]
    public DateOnly RecordDate { get; set; }

    [Required(ErrorMessage = "Введите заголовок")]
    [MaxLength(300, ErrorMessage = "Заголовок не должен превышать 300 символов")]
    [Display(Name = "Заголовок")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Комментарий не должен превышать 2000 символов")]
    [Display(Name = "Комментарий")]
    [DataType(DataType.MultilineText)]
    public string? Comment { get; set; }

    [Display(Name = "Прикрепить файл (PDF, JPG, PNG, до 10 МБ)")]
    public IFormFile? File { get; set; }

    // Для лабораторных анализов
    public bool IsLabTest { get; set; }
    public List<ParameterInputViewModel> Parameters { get; set; } = new();

    // Для выпадающего списка категорий
    public List<CategorySelectViewModel> Categories { get; set; } = new();
}

public class ParameterInputViewModel
{
    public int TestParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal NormMin { get; set; }
    public decimal NormMax { get; set; }
    public decimal? Value { get; set; }

    // Флаги для подсветки
    public bool IsOutOfRange => Value.HasValue && (Value < NormMin || Value > NormMax);
    public bool IsNearOutOfRange => !IsOutOfRange && Value.HasValue &&
        ((Value <= NormMin * 1.1m && Value >= NormMin) ||
         (Value >= NormMax * 0.9m && Value <= NormMax));
}

public class CategorySelectViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLabTest { get; set; }
}