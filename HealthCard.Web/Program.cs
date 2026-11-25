using Data;
using HealthCard.Services;
using HealthCard.Services.Interfaces;
using HealthCard.Web.Infrastructure;
using HealthCard.Web.Services;
using Microsoft.EntityFrameworkCore;
using Models.UserSettings;
using Services;
using Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
namespace HealthCard.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        if (builder.Environment.IsEnvironment("Testing"))
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Configuration["Storage:Root"] ?? Path.Combine(Path.GetTempPath(), "HealthCardQA"), "keys")));
        builder.Services.AddControllersWithViews(options => options.ModelBinderProviders.Insert(0, new FlexibleDecimalBinderProvider()));
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true; options.Password.RequireLowercase = true; options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false; options.Password.RequiredLength = 6;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddErrorDescriber<RussianIdentityErrors>();
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login"; options.LogoutPath = "/Account/Logout"; options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7); options.Cookie.HttpOnly = true;
        });
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<IResultEvaluationService, ResultEvaluationService>();
        builder.Services.AddScoped<RecordQueryService>();
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await DataSeeder.InitializeAsync(context);
        }
        if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
        if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
        // Legacy attachments are only served by the owner-checked Download action.
        app.Use(async (context, next) =>
        {
            if (app.Environment.IsEnvironment("Testing")) context.Response.Headers["X-HealthCard-Environment"] = "Testing";
            if (context.Request.Path.StartsWithSegments("/uploads")) { context.Response.StatusCode = 404; return; }
            await next(context);
        });
        app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}").WithStaticAssets();
        await app.RunAsync();
    }
}
