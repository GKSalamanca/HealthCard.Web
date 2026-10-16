using Models.UserSettings;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCard.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            // Проверяем, существует ли пользователь
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                // Пользователь не найден
                ModelState.AddModelError(string.Empty, "Пользователь не зарегистрирован. Пожалуйста, зарегистрируйтесь.");
                return View(model);
            }

            // Пытаемся войти
            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Аккаунт заблокирован. Попробуйте позже.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверный пароль. Попробуйте еще раз.");
            }
        }

        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Проверяем, не зарегистрирован ли уже такой email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Пользователь с таким Email уже зарегистрирован.");
                return View(model);
            }

            var user = new ApplicationUser(model.Email, model.FullName);
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Успешная регистрация
                TempData["SuccessMessage"] = "Пользователь зарегистрирован. Нажмите 'Войти' для входа в систему.";
                return RedirectToAction("Login");
            }

            // Если были ошибки
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    // GET: /Account/AccessDenied
    public IActionResult AccessDenied()
    {
        return View();
    }
}