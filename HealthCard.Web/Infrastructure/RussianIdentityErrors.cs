using Microsoft.AspNetCore.Identity;
namespace HealthCard.Web.Infrastructure;

public class RussianIdentityErrors : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string message) => new() { Code = code, Description = message };
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"Пароль должен содержать не менее {length} символов");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "Добавьте в пароль цифру");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "Добавьте в пароль строчную латинскую букву");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "Добавьте в пароль прописную латинскую букву");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Текущий пароль неверен");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "Этот email уже зарегистрирован");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "Этот email уже зарегистрирован");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "Проверьте формат email");
}
