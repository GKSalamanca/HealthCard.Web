using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models.UserSettings;
using ViewModels.Entrance;
namespace HealthCard.Web.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    { ViewData["ReturnUrl"] = returnUrl; return View(new LoginViewModel()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        var result = user == null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/"));
        ModelState.AddModelError("", result.IsLockedOut ? "Слишком много попыток входа. Повторите через несколько минут." : "Неверный email или пароль");
        return View(model);
    }
    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await users.FindByEmailAsync(model.Email.Trim()) != null) ModelState.AddModelError("Email", "Этот email уже зарегистрирован");
            else
            {
                var user = new ApplicationUser(model.Email.Trim(), model.FullName.Trim());
                var result = await users.CreateAsync(user, model.Password);
                if (result.Succeeded) { TempData["SuccessMessage"] = "Вы зарегистрированы. Войдите в свою карточку здоровья."; return RedirectToAction(nameof(Login)); }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }
    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new ProfileViewModel { FullName = user.FullName, Email = user.Email ?? "", DateOfBirth = user.DateOfBirth, Gender = user.Gender, BloodType = user.BloodeType });
    }
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        model.Email = user.Email ?? "";
        if (ModelState.IsValid)
        {
            user.UpdateProfile(model.FullName.Trim(), model.DateOfBirth, model.Gender, string.IsNullOrEmpty(model.BloodType) ? null : model.BloodType);
            var result = await users.UpdateAsync(user);
            if (result.Succeeded) { await signIn.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Профиль сохранен"; return RedirectToAction(nameof(Profile)); }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }
    [Authorize, HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordViewModel());
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await users.GetUserAsync(User); if (user == null) return Challenge();
        if (ModelState.IsValid)
        {
            var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded) { await signIn.RefreshSignInAsync(user); TempData["SuccessMessage"] = "Пароль изменен"; return RedirectToAction(nameof(Profile)); }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [HttpGet] public IActionResult AccessDenied() => View();
}
