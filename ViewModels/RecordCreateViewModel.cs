using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
namespace HealthCard.Web.ViewModels;

public class RecordCreateViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию")] public int CategoryId { get; set; }
    [DataType(DataType.Date)] public DateOnly RecordDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "Введите заголовок"), MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string? Comment { get; set; }
    public IFormFile? File { get; set; }
    public bool RemoveFile { get; set; }
    [ValidateNever] public string? ExistingFilePath { get; set; }
    [ValidateNever] public bool IsLabTest { get; set; }
    public List<ParameterInputViewModel> Parameters { get; set; } = new();
    [ValidateNever] public List<CategorySelectViewModel> Categories { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (RecordDate == default || RecordDate > DateOnly.FromDateTime(DateTime.Today))
            yield return new("Дата должна быть не позднее сегодняшнего дня", new[] { nameof(RecordDate) });
    }
}
public class ParameterInputViewModel
{
    public int TestParameterId { get; set; }
    [ValidateNever] public string ParameterName { get; set; } = "";
    [ValidateNever] public string Unit { get; set; } = "";
    [ValidateNever] public decimal NormMin { get; set; }
    [ValidateNever] public decimal NormMax { get; set; }
    [Range(0, 99999999, ErrorMessage = "Допустимое значение от 0 до 99999999")] public decimal? Value { get; set; }
}
public class CategorySelectViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsLabTest { get; set; }
}
