using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Models.ParameterBase
{
    //Результат анализа
    public class TestResult
    {
        [Key]
        public int Id { get; private set; }

        public int MedicalRecordId {  get; private set; }
        [ForeignKey(nameof(MedicalRecordId))]
        public virtual MedicalRecord MedicalRecord { get; private set; } = null!;

        public int TestParameterId { get; private set; }
        [ForeignKey(nameof(TestParameterId))]
        public virtual TestParameter TestParameter { get; private set;} = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Value { get; private set;  }

        private TestResult() 
        {
        }

        public static TestResult Create(int medicalRecordId, int testParameterId, decimal value)
        {
            if (value < 0)
                throw new ArgumentException("Значение не может быть отрицательным", nameof(value));
            return new TestResult
            {
                MedicalRecordId = medicalRecordId,
                TestParameterId = testParameterId,
                Value = Math.Round(value,2) // Автоматическое округление до 2 знаков
            };
        }

        public void UpdateValue(decimal value)
        {
            if(value < 0)
                throw new ArgumentException("Значение не может быть отрицательным",nameof(value));
            Value = Math.Round(value,2);
        }
    }
}
