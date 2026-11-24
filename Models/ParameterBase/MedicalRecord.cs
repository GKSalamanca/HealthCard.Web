using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Models.UserSettings;

namespace Models.ParameterBase
{
    public class MedicalRecord
    {
        [Key] 
        public int Id { get; private set; }                                  //Первичный ключ
        [Required]                                                           //Гарантия что у каждой записи всегда есть владелец
        public string UserId { get; private set; } = string.Empty;           //Внешний ключ на пользователя
        [ForeignKey(nameof(UserId))]                                         // Навигационное свойство к пользователю. Явно указываем Внешний ключ на пользователя
        public virtual ApplicationUser User { get; set; } = null!; 
        public int CategoryId { get; private set; }                          //Внешний ключ на категорию
        [ForeignKey(nameof(CategoryId))]                                     //Навигационное свойство к категории.
        public virtual Category Category { get; private set; } = null!;      //Всегда будет заполнено из базы по внешнему ключу явно указаному в атрибуте
        public DateOnly RecordDate { get; private set; }                     // Дата записи
        [Required, MaxLength(300)]
        public string Title {  get; private set; } = string.Empty;           // Заголовок записи
        [MaxLength(2000)]
        public string? Comment { get; private set; }                         // Комментарий пользователя
        [MaxLength(500)]
        public string? FilePath { get; private set; }                        // Путь к загруженному файлу (PDF, JPG). Необязательное поле.
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;   // Дата и время создания записи в системе (автоматически).


        public virtual ICollection<TestResult> TestResults { get; private set; } = new List<TestResult>(); //Навигационная коллекция (у одной записи множество результатов)

        private MedicalRecord() 
        {
        }
        //Фабрика
        public static  MedicalRecord Create(string userId, int categoryId, DateOnly recordDate, string title, string? comment)
        {
            //Все эти проверки это Валидация
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Идентификатор пользователя не может быть пустым", nameof(userId));
            if(string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Заголовок не может быть пустым",nameof(title));
            if (recordDate > DateOnly.FromDateTime(DateTime.Today))
                throw new ArgumentException("",nameof(recordDate));

            return new MedicalRecord
            {
                UserId = userId,
                CategoryId = categoryId,
                RecordDate = recordDate,
                Title = title.Trim(),
                Comment = comment?.Trim(),
                CreatedAt = DateTime.UtcNow // Фиксируем время создания
            };
        }

        // Обновление основных текстовых данных и даты
        public void Update(DateOnly recordDate, string title, string? comment)
        {
            if(string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Заголовок не может быть пустым",nameof(title));

            RecordDate = recordDate;
            Title = title.Trim();
            Comment = comment?.Trim();
        }
        // Отдельный метод для установки пути к файлу.
        public void SetFilePath(string? filePath)
        {
            FilePath = filePath;
        }
        // Добавление результата анализа в эту запись
        public void AddResult(TestResult result)
        {
            if(result == null)
                throw new ArgumentNullException(nameof(result));
            TestResults.Add(result);
        }
        // Удаление результата анализа из этой записи
        public void RemoveResult(TestResult result)
        {
            if(result == null)
                throw new ArgumentNullException(nameof(result));
            TestResults.Remove(result);
        }
    }
}
