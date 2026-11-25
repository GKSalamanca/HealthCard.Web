using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Models.Enum;
using Models.ParameterBase;

namespace HealthCard.Web.ViewModels;

public class CategoryEditViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Введите название"), MaxLength(200)]
    public string Name { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public bool IsLabTest { get; set; } = true;
    public List<CategoryParameterViewModel> Parameters { get; set; } = new();
    [ValidateNever] public bool HasRecords { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!IsLabTest) yield break;
        if (IsLabTest && !Parameters.Any(p => !p.Remove))
            yield return new("Добавьте хотя бы один показатель анализа", new[] { nameof(Parameters) });
        var names = Parameters.Where(p => !p.Remove).Select(p => (p.Name?.Trim() ?? "")).ToList();
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
            yield return new("Названия показателей не должны повторяться", new[] { nameof(Parameters) });
    }
}

public class CategoryParameterViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Название показателя обязательно"), MaxLength(200)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Укажите единицу измерения"), MaxLength(50)] public string Unit { get; set; } = "";
    [Range(0, 99999999)] public decimal NormMin { get; set; }
    [Range(0, 99999999)] public decimal NormMax { get; set; }
    public bool Remove { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Remove && NormMin > NormMax)
            yield return new("Нижняя граница не может превышать верхнюю", new[] { nameof(NormMax) });
    }
}

public class ProfileViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Введите ФИО"), MaxLength(200)] public string FullName { get; set; } = "";
    [ValidateNever] public string Email { get; set; } = "";
    [DataType(DataType.Date)] public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? BloodType { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            yield return new("Дата рождения не может быть в будущем", new[] { nameof(DateOfBirth) });
        if (Gender.HasValue && !Enum.IsDefined(Gender.Value))
            yield return new("Выберите пол из списка", new[] { nameof(Gender) });
        if (BloodType is not null && !new[] { "", "O(I)+", "O(I)-", "A(II)+", "A(II)-", "B(III)+", "B(III)-", "AB(IV)+", "AB(IV)-" }.Contains(BloodType))
            yield return new("Выберите группу крови из списка", new[] { nameof(BloodType) });
    }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль"), DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Введите новый пароль"), StringLength(100, MinimumLength = 6), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают"), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}

public class NormEditViewModel : IValidatableObject
{
    public int TestParameterId { get; set; }
    [ValidateNever] public string Name { get; set; } = "";
    [ValidateNever] public string Unit { get; set; } = "";
    [ValidateNever] public decimal DefaultMin { get; set; }
    [ValidateNever] public decimal DefaultMax { get; set; }
    [Range(0, 99999999)] public decimal NormMin { get; set; }
    [Range(0, 99999999)] public decimal NormMax { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (NormMin > NormMax) yield return new("Нижняя граница не может превышать верхнюю", new[] { nameof(NormMax) });
    }
}

public class NormListViewModel
{
    public int? CategoryId { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<NormListRow> Parameters { get; set; } = new();
}
public record NormListRow(int Id, string Name, string Unit, string Category, decimal DefaultMin, decimal DefaultMax, decimal? CustomMin, decimal? CustomMax);

public class ResultRowViewModel
{
    public int RecordId { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ParameterId { get; set; }
    public string ParameterName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Value { get; set; }
    public decimal NormMin { get; set; }
    public decimal NormMax { get; set; }
    public bool IsCustomNorm { get; set; }
    public ResultStatus Status { get; set; }
    public string StatusText => Status switch { ResultStatus.OutOfRange => "Вне интервала", ResultStatus.NearBoundary => "У границы", _ => "В интервале" };
    public string CssClass => Status switch { ResultStatus.OutOfRange => "danger", ResultStatus.NearBoundary => "warning", _ => "success" };
}
public class RecordDetailsViewModel
{
    public MedicalRecord Record { get; set; } = null!;
    public List<ResultRowViewModel> Results { get; set; } = new();
}
public class RecordListViewModel
{
    public int? CategoryId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<MedicalRecord> Records { get; set; } = new();
}
public class CategoryRecordsViewModel
{
    public Category Category { get; set; } = null!;
    public List<MedicalRecord> Records { get; set; } = new();
    public List<TestParameter> Parameters { get; set; } = new();
    public Dictionary<(int RecordId, int ParameterId), ResultRowViewModel> Results { get; set; } = new();
}
public class AnalyticsViewModel
{
    public int? CategoryId { get; set; }
    public int? ParameterId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<TestParameter> Parameters { get; set; } = new();
    public int TotalRecords { get; set; }
    public List<ResultRowViewModel> Results { get; set; } = new();
    public List<ResultRowViewModel> Series { get; set; } = new();
    public int OutOfRangeCount => Results.Count(r => r.Status == ResultStatus.OutOfRange);
    public int NearBoundaryCount => Results.Count(r => r.Status == ResultStatus.NearBoundary);
    public decimal? Minimum => Series.Count == 0 ? null : Series.Min(r => r.Value);
    public decimal? Maximum => Series.Count == 0 ? null : Series.Max(r => r.Value);
    public decimal? Average => Series.Count == 0 ? null : Series.Average(r => r.Value);
    public decimal? Change => Series.Count < 2 ? null : Series[^1].Value - Series[0].Value;
}
