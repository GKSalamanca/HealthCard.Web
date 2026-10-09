using Microsoft.AspNetCore.Identity;
using Models.Enum;
using Models.ParameterBase;



namespace Models.UserSettings
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; private set; } = string .Empty; // ФИО пользователя
        public DateOnly? DateOfBirth { get; private set; } //Дата рождения
        public  Gender? Gender { get; private set; } // Пол пользователя
        public string? BloodeType { get; private set; } //Группа крови
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow; // Дата регистрации пользователя


        /*
             Для себя что бы не забыть (ICollection<T> интерфейс, который описывает, как должна вести себя любая коллекция,
        хранящая однотипные элементы, предоставляя базовый набор операций для работы с ними)***
        */
        public virtual ICollection<MedicalRecord> MedicalRecords { get; private set; } = new List<MedicalRecord>();
        public virtual ICollection<UserParameterNorm> UserParameterNorms { get; private set; } = new List<UserParameterNorm>();

        //Пустой конструктор  для EF (когда читает данные из базы вызывает создает объект через этот конструктор)
        // EF Core сам заполнит все свойства через рефлексию***
        private ApplicationUser()
        {

        }

        public ApplicationUser(string email, string fullName)
        {
            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException("Адрес электронной почты не может быть пустым", nameof(email));
            if(string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException("ФИО не может быть пустым",nameof(fullName));

            Email = email;
            UserName = email; //Логин
            FullName = fullName;
            CreatedAt = DateTime.UtcNow;
        }

        // Метод обновления профиля пользователя
        public void UpdateProfile(string fullname, DateOnly? dateOfBirth, Gender? gender, string? bloodtype)
        {
            if(string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentNullException("ФИО не может быть пустым", nameof(fullname));

            FullName = fullname;
            DateOfBirth = dateOfBirth;
            Gender = gender;
            BloodeType = bloodtype;
        }
        // Метод добавления медицинской записи (контролирую , как добавляются записи)
        public void AddMedicalRecord(MedicalRecord record)
        {
            if(record == null)
                throw new ArgumentNullException(nameof(record));
            MedicalRecords.Add(record);
        }
    }
}
