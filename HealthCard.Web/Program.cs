using Data;
using HealthCard.Services;
using HealthCard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Models.UserSettings;
using Services;
using Services.Interfaces;

namespace HealthCard.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Настройка БД (PostgreSQL)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. Регистрация Identity
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // 3. Настройка Cookie для правильного редиректа на страницу входа
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";          // Куда редиректить, если не авторизован
                options.LogoutPath = "/Account/Logout";        // Путь для выхода
                options.AccessDeniedPath = "/Account/AccessDenied"; // Путь при отказе в доступе
                options.ExpireTimeSpan = TimeSpan.FromDays(7); // Время жизни куки
            });

            // 4. РЕГИСТРАЦИЯ НАШИХ СЕРВИСОВ
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            
            // Регистрация сервисов
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

            var app = builder.Build();

            // 5. НАЧАЛО БЛОКА SEED ДАННЫХ (Отличная реализация!)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Гарантируем применение миграций
                    await context.Database.MigrateAsync();

                    // Заполняем справочники
                    await DataSeeder.InitializeAsync(context);

                    Console.WriteLine("✅ База данных успешно инициализирована и заполнена!");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Произошла ошибка при заполнении базы данных начальными данными.");
                }
            }
            // КОНЕЦ БЛОКА SEED ДАННЫХ

            // 6. Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // ВАЖНО: Разрешаем отдачу статических файлов (нужно для загрузки PDF/картинок в wwwroot/uploads)
            app.UseStaticFiles();

            app.UseRouting();

            // ВАЖНО: Порядок имеет значение! Сначала аутентификация (чтение куки), потом авторизация (проверка прав)
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // 7. МАРШРУТ ПО УМОЛЧАНИЮ
            // Меняем Home на Dashboard, чтобы при запуске открывалась главная страница приложения
            // (Если пользователь не авторизован, его автоматически перекинет на /Account/Login благодаря настройкам Cookie выше)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Dashboard}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}