using Models.UserSettings;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ParameterBase
{
    public class Category
    {
        [Key] //Идентификатор (Примечание для себя* указывает Entity Framework Core, что это Первичный ключ (Primary Key))
        public int Id { get; private set; }
        public string? UserId { get; private set; } //Внешний ключ на пользователя, создается системой 
        // Навигационное свойство. virtual позволяет EF Core использовать "ленивую загрузку" (Lazy Loading)***
        // или создавать прокси-объекты для отслеживания изменений.
        public virtual ApplicationUser? User { get; private set; } 

        [Required, MaxLength(200)] // [Required] - поле обязательно для заполнения в БД [MaxLength(200)] - ограничивает длину строки в базе данных
        public string Name { get; private set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description {  get; private set; }
        // Флаг: true, если это лабораторный анализ (ОАК, Биохимия), false - если это просто файл (УЗИ, Рентген)
        public bool IsLabTest {  get; private set; }

        [MaxLength(100)]
        public string? IconClass {  get; private set; } // Например, "bi bi-heart-pulse" для Bootstrap Icons
        public int SortOrder { get; private set; } // Порядок сортировки категорий при отображении
        /* 
         * Флаг защиты: true, если категория создана системой (из Seed-данных). 
         * Такие категории нельзя удалять или менять их базовые параметры.
        */
        public bool IsSystem {  get; private set; }

        /*
         * НАВИГАЦИОННЫЕ КОЛЛЕКЦИИ (СВЯЗИ 1 КО МНОГИМ)
         * У одной категории может быть много параметров(Гемоглобин, Лейкоциты и т.д.)
         * Инициализируем пустым списком, чтобы избежать NullReferenceException
        */
        public virtual ICollection<TestParameter> TestParameters { get; private set; } = new List<TestParameter>();
        // У одной категории может быть много медицинских записей пользователей
        public virtual ICollection<MedicalRecord> MedicalRecords { get; private set; } = new List<MedicalRecord>();

        private Category() 
        {

        }

        //Ознакомился тестирую здесь фабрику
        // ФАБРИЧНЫЕ МЕТОДЫ (FACTORY METHODS)
        // Вместо того чтобы разрешать new Category(), мы заставляем создавать 
        // объекты через эти методы. Это гарантирует, что объект всегда создан в ВАЛИДНОМ состоянии.
        // 1. Создание СИСТЕМНОЙ категории
        public static Category CreateSystem(string name, bool isLabtest, int sortOrder, string? description = null, string? iconClass = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("Имя не должно быть пустым",nameof(name));
            return new Category
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                IsLabTest = isLabtest,
                IconClass = iconClass,
                SortOrder = sortOrder,
                IsSystem = true,
                UserId = null
            };
        }
        // 2. Создание ПОЛЬЗОВАТЕЛЬСКОЙ категории
        public static Category CreateUser(string userId, string name, bool isLabTest, int sortOrder, string? description = null, string? iconClass = null)
        {
            if(string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException("Идентификатор пользователя не может быть пустым",nameof(userId));
            if(string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("Имя не может быть пустым",nameof(name));
            return new Category
            {
                UserId = userId,
                Name = name.Trim(),
                Description = description?.Trim(),
                IsLabTest = isLabTest,
                IconClass = iconClass,
                SortOrder = sortOrder,
                IsSystem = false
            };
        }
        // МЕТОДЫ ИЗМЕНЕНИЯ СОСТОЯНИЯ (МУТАТОРЫ)
        // Метод обновления данных категории
        public void Update (string name, string? description, string? iconClass)
        {
            if (IsSystem)
                throw new InvalidOperationException("Нельзя изменить системную категорию");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя не может быть пустым", nameof(name));
            Name = name.Trim();
            Description = description?.Trim();
            IconClass = iconClass;
        }
        // Отдельный метод для изменения порядка сортировки
        public void SetSortOrder(int sortOrder)
        {
            SortOrder = sortOrder;
        }
        // Метод добавления параметра в категорию
        public void AddParameter(TestParameter parameter)
        {
            if(parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            TestParameters.Add(parameter);
        }
    }
}
