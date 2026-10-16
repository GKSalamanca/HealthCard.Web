using System.ComponentModel.DataAnnotations;

namespace HealthCard.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите ФИО")]
    [MaxLength(200, ErrorMessage = "ФИО не должно превышать 200 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите Email")]
    [EmailAddress(ErrorMessage = "Неверный формат Email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [StringLength(100, ErrorMessage = "Пароль должен содержать от {2} до {1} символов", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Подтвердите пароль")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}