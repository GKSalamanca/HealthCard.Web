using Models.ParameterBase;
using Models.UserSettings;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        //Таблицы базы данных
        public DbSet<Category> Categories { get; set; }
        public DbSet<TestParameter> TestParameters { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<UserParameterNorm> UserParameterNorms { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Обязательно вызываем базовый метод для настройки Identity
            base.OnModelCreating(builder);
            // 1. УНИКАЛЬНЫЙ ИНДЕКС для UserParameterNorm
            builder.Entity<UserParameterNorm>()
                .HasIndex(upn => new { upn.UserId, upn.TestParameterId })         // Создаём индекс по двум полям сразу: UserId и TestParameterId
                .IsUnique()                                                       // Делаем этот индекс уникальным (Unique)
                .HasDatabaseName("IX_UserParameterNorm_UserId_TestParameterId");  // Задаём понятное имя индексу в базе данных (для удобства отладки)
            // 2. СВЯЗИ (RELATIONSHIPS) - Fluent API
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.User)                                // У записи есть ОДИН пользователь
                .WithMany(u => u.MedicalRecords)                      // У пользователя есть МНОГО записей
                .HasForeignKey(MedicalRecord => MedicalRecord.UserId) // Внешний ключ находится в таблице MedicalRecord в поле UserId
                .OnDelete(DeleteBehavior.Cascade);                    // ПРАВИЛО: Если удалили Пользователя -> удаляются ВСЕ его записи.
            // Связь: MedicalRecord (Многие) -> Category (Один)
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Category)
                .WithMany(c => c.MedicalRecords)
                .HasForeignKey(mr => mr.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            // 💡 Это защищает от ситуации, когда пользователь удаляет категорию "ОАК", а его старые записи остаются "висящими в воздухе".
            // Связь: TestResult (Многие) -> MedicalRecord (Один)
            builder.Entity<TestResult>()
                .HasOne(tr =>  tr.MedicalRecord)
                .WithMany(mr => mr.TestResults)
                .HasForeignKey(tr => tr.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            // Связь: TestResult (Многие) -> TestParameter (Один)
            builder.Entity<TestResult>()
                .HasOne(tr => tr.TestParameter)
                .WithMany(tp => tp.TestResults)
                .HasForeignKey (tr => tr.TestParameterId)
                .OnDelete (DeleteBehavior.Restrict);
            // Связь: UserParameterNorm (Многие) -> ApplicationUser (Один)
            builder.Entity<UserParameterNorm>()
                .HasOne(upn => upn.User)
                .WithMany(u => u.UserParameterNorms)
                .HasForeignKey(upn => upn.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Связь: UserParameterNorm (Многие) -> TestParameter (Один)
            builder.Entity<UserParameterNorm>()
                .HasOne(upn => upn.TestParameter)
                .WithMany(u => u.UserParameterNorms)
                .HasForeignKey(upn => upn.TestParameterId)
                .OnDelete(DeleteBehavior.Cascade);
            // Связь: Category (Многие) -> ApplicationUser (Один) - для пользовательских категорий
            builder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany(u => u.UserCategories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull); // ПРАВИЛО: Если пользователь удалил свой аккаунт, его кастомные категории НЕ удаляются, а их поле UserId становится NULL. 

        }
    }
}
