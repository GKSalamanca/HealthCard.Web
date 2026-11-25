using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Models.ParameterBase;

namespace Models.UserSettings
{
    public class UserParameterNorm
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        public string UserId { get; private set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; private set; } = null!;

        public int TestParameterId { get; private set; }
        [ForeignKey(nameof(TestParameterId))]
        public virtual TestParameter TestParameter { get; private set; } = null!;
        //Персональная нижняя граница нормы
        [Column(TypeName = "numeric(12,4)")]
        public decimal CustomNormMin { get; private set; }
        //Персональная верхняя граница нормы
        [Column(TypeName = "numeric(12,4)")]
        public decimal CustomNormMax { get; private set; }

        private UserParameterNorm()
        {
        }

        public static UserParameterNorm Create(string userId, int testParameterId, decimal customNormMin, decimal customNormMax)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Идентификатор не может быть пустым", nameof(userId));
            if (customNormMin < 0 || customNormMax < 0)
                throw new ArgumentException("Границы нормы не могут быть отрицательными");
            if (customNormMin > customNormMax)
                throw new ArgumentException("Нижняя граница нормы не может быть больше верхней границы");

            return new UserParameterNorm
            {
                UserId = userId,
                TestParameterId = testParameterId,
                CustomNormMin = Math.Round(customNormMin, 4),
                CustomNormMax = Math.Round(customNormMax, 4)
            };
        }

        // Метод обновления норм
        public void UpdateNorms(decimal customNormMin, decimal customNormMax)
        {
            if (customNormMin < 0 || customNormMax < 0)
                throw new ArgumentException("Границы нормы не могут быть отрицательными");
            if (customNormMin > customNormMax)
                throw new ArgumentException("Нижняя граница нормы не может быть больше верхней границы");

            CustomNormMin = Math.Round(customNormMin, 4);
            CustomNormMax = Math.Round(customNormMax, 4);
        }
    }
}
