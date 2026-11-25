using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Models.UserSettings;

namespace Models.ParameterBase
{
    public class TestParameter
    {
        [Key]
        public int Id { get; private set; }
        // Внешний ключ.Хранит ID категории, к которой принадлежит этот параметр.
        public int CategoryId { get; private set; }

        // Навигационное свойство (ссылка на родителя).[ForeignKey] явно указывает EF Core, какое поле является внешним ключом.
        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; private set; } = null!;
        [Required, MaxLength(200)]
        public string Name { get; private set; } = string.Empty; // Название параметра
        [Required, MaxLength(50)]
        public string Unit { get; private set; } = string.Empty; // Единица измерения
        // Нижняя и верхняя границы нормы. ВАЖНО***: Для денег и медицинских показателей ВСЕГДА используем decimal 
        // numeric(12,4): 12 цифр, из которых 4 после запятой.
        [Column(TypeName = "numeric(12,4)")]
        public decimal NormMin { get; private set; }
        [Column(TypeName = "numeric(12,4)")]
        public decimal NormMax { get; private set; }
        public int SortOrder { get; private set; } // Порядок сортировки
        public bool IsSystem { get; private set; } // Флаг Системный параметр или пользовательский

        public virtual ICollection<TestResult> TestResults { get; private set; } = new List<TestResult>();
        public virtual ICollection<UserParameterNorm> UserParameterNorms { get; private set; } = new List<UserParameterNorm>();

        private TestParameter()
        {

        }

        public static TestParameter Create(int categoryId, string name, string unit, decimal normMin, decimal normMax, int sortOrder, bool isSystem = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название параметра не может быть пустым", nameof(name));
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Единица измерения не может быть пустой", nameof(unit));
            if (normMin < 0 || normMax < 0)
                throw new ArgumentException("Границы нормы не могут быть отрицательными", nameof(normMin));
            if (normMin > normMax)
                throw new ArgumentException("Минимальная граница нормы не может быть больше максимальной", nameof(normMax));

            return new TestParameter
            {
                CategoryId = categoryId,
                Name = name.Trim(),
                Unit = unit.Trim(),
                NormMin = normMin,
                NormMax = normMax,
                SortOrder = sortOrder,
                IsSystem = isSystem
            };
        }

        public void UpdateNorms(decimal normMin, decimal normMax)
        {
            if (normMin < 0 || normMax < 0)
                throw new ArgumentException("Границы нормы не могут быть отрицательными", nameof(normMin));
            if (normMin > normMax)
                throw new ArgumentException("Минимальная граница нормы не может быть больше максимальной", nameof(normMax));
            NormMin = normMin;
            NormMax = normMax;
        }

        public void Update(string name, string unit)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название не может быть пустым", nameof(name));
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Единица измерения не может быть пустой", nameof(unit));

            Name = name.Trim();
            Unit = unit.Trim();
        }
        public void SetSortOrder(int order) => SortOrder = order;
    }
}
