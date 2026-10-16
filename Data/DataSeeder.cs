using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Models.ParameterBase;

namespace Data
{
    public static class DataSeeder
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Если категории уже есть, значит база уже заполнена (защита от повторного сидирования)
            if (await context.Categories.AnyAsync())
            {
                return;
            }

            var categories = new List<Category>
                {
                    Category.CreateSystem("Общий анализ крови (ОАК)", true, 1, "Расширенный общий анализ крови", "bi bi-droplet"),
                    Category.CreateSystem("Биохимический анализ крови", true, 2, "Расширенная биохимия", "bi bi-activity"),
                    Category.CreateSystem("Липидный профиль", true, 3,"Холестерин и фракции","bi bi-heart-pulce"),
                    Category.CreateSystem("Гормоны щитовидной железы", true, 4,"ТТГ, Т3, Т4 и антитела", "bi bi-shield-check"),
                    Category.CreateSystem("Общий анализ мочи (ОАМ)", true, 5, "Клинический анализ мочи", "bi bi-cup"),
                    Category.CreateSystem("УЗИ", false, 6,"Ультрозвуковое исследование", "bi bi-display"),
                    Category.CreateSystem("МРТ/КТ", false, 7, "Магнитно-резонансная и компьютерная томография", "bi bi-cpu"),
                    Category.CreateSystem("ЭКГ", false, 8, "Элеткрокардиограмма","bi bi-heart-pulse-fill"),
                    Category.CreateSystem("Заключение врача", false, 9,"Текстовое заключения специалистов", "bi bi-file-earmark-medical"),
                    Category.CreateSystem("Рентген", false,10,"Рентгенографические исследования", "bi bi-file-earmark-image"),
                    Category.CreateSystem("Флюрография", false, 11, "Профилактическое рентгеновское исследование", "bi bi-shield-shaded")
                };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync(); // Сохраняем, чтобы сгенерировались Id категорий

            // 2. Заполняем параметры для лабораторных тестов
            await SeedOAK(context, categories[0].Id);
            await SeedBiochemistry(context, categories[1].Id);
            await SeedLipidProfile(context, categories[2].Id);
            await SeedThyroid(context, categories[3].Id);
            await SeedOAM(context, categories[4].Id);

            await context.SaveChangesAsync();
        }

        private static async Task SeedOAK(ApplicationDbContext context, int categoryId)
        {
                var parameters = new List<TestParameter>
            {
                TestParameter.Create(categoryId, "Количество лейкоцитов", "10^9/л", 4.00m, 9.00m, 1),
                TestParameter.Create(categoryId, "Количество эритроцитов", "10^12/л", 3.80m, 5.50m, 2),
                TestParameter.Create(categoryId, "Гемоглобин", "г/л", 120.00m, 160.00m, 3),
                TestParameter.Create(categoryId, "Гематокрит", "%", 35.00m, 47.00m, 4),
                TestParameter.Create(categoryId, "Средний объем эритроцитов (MCV)", "фл", 80.00m, 100.00m, 5),
                TestParameter.Create(categoryId, "Среднее содержание Hb в эритроците (MCH)", "пг", 26.00m, 34.00m, 6),
                TestParameter.Create(categoryId, "Средняя концентрация Hb в эритроците (MCHC)", "г/л", 300.00m, 380.00m, 7),
                TestParameter.Create(categoryId, "Ширина распределения эритроцитов (RDW)", "%", 11.50m, 14.50m, 8),
                TestParameter.Create(categoryId, "Количество тромбоцитов", "10^9/л", 150.00m, 450.00m, 9),
                TestParameter.Create(categoryId, "Средний объем тромбоцитов (MPV)", "фл", 7.40m, 10.40m, 10),
                TestParameter.Create(categoryId, "Нейтрофилы (процент)", "%", 48.00m, 78.00m, 11),
                TestParameter.Create(categoryId, "Лимфоциты (процент)", "%", 19.00m, 37.00m, 12),
                TestParameter.Create(categoryId, "Моноциты (процент)", "%", 3.00m, 11.00m, 13),
                TestParameter.Create(categoryId, "Эозинофилы (процент)", "%", 0.02m, 5.00m, 14),
                TestParameter.Create(categoryId, "Базофилы (процент)", "%", 0.00m, 1.00m, 15),
                TestParameter.Create(categoryId, "Нейтрофилы (абсолютные)", "10^9/л", 2.04m, 5.80m, 16),
                TestParameter.Create(categoryId, "Лимфоциты (абсолютные)", "10^9/л", 1.20m, 3.00m, 17),
                TestParameter.Create(categoryId, "Моноциты (абсолютные)", "10^9/л", 0.09m, 0.60m, 18),
                TestParameter.Create(categoryId, "Эозинофилы (абсолютные)", "10^9/л", 0.02m, 0.30m, 19),
                TestParameter.Create(categoryId, "Базофилы (абсолютные)", "10^9/л", 0.00m, 0.07m, 20),
                TestParameter.Create(categoryId, "Нейтрофильно-лимфоцитарное соотношение (NLR)", "ед.", 0.20m, 3.50m, 21),
                TestParameter.Create(categoryId, "СОЭ по Вестергрену", "мм/ч", 2.00m, 20.00m, 22)
            };
            await context.TestParameters.AddRangeAsync(parameters);
        }

        private static async Task SeedBiochemistry(ApplicationDbContext context, int categoryId)
        {
            var parameters = new List<TestParameter>
            {
                TestParameter.Create(categoryId, "Глюкоза", "ммоль/л", 3.30m, 5.50m, 1),
                TestParameter.Create(categoryId, "Общий белок", "г/л", 64.00m, 83.00m, 2),
                TestParameter.Create(categoryId, "Альбумин", "г/л", 35.00m, 52.00m, 3),
                TestParameter.Create(categoryId, "Билирубин общий", "мкмоль/л", 3.40m, 17.10m, 4),
                TestParameter.Create(categoryId, "Билирубин прямой", "мкмоль/л", 0.00m, 3.40m, 5),
                TestParameter.Create(categoryId, "АЛТ", "Ед/л", 0.00m, 40.00m, 6),
                TestParameter.Create(categoryId, "АСТ", "Ед/л", 0.00m, 40.00m, 7),
                TestParameter.Create(categoryId, "Креатинин", "мкмоль/л", 44.00m, 97.00m, 8),
                TestParameter.Create(categoryId, "Мочевина", "ммоль/л", 2.50m, 8.30m, 9),
                TestParameter.Create(categoryId, "Мочевая кислота", "мкмоль/л", 200.00m, 420.00m, 10),
                TestParameter.Create(categoryId, "Калий", "ммоль/л", 3.50m, 5.50m, 11),
                TestParameter.Create(categoryId, "Натрий", "ммоль/л", 135.00m, 145.00m, 12),
                TestParameter.Create(categoryId, "Железо сывороточное", "мкмоль/л", 11.60m, 30.40m, 13),
                TestParameter.Create(categoryId, "Ферритин", "нг/мл", 20.00m, 250.00m, 14),
                TestParameter.Create(categoryId, "С-реактивный белок", "мг/л", 0.00m, 5.00m, 15)
            };
            await context.TestParameters.AddRangeAsync(parameters);
        }

        private static async Task SeedLipidProfile(ApplicationDbContext context, int categoryId)
        {
            var parameters = new List<TestParameter>
            {
                TestParameter.Create(categoryId, "Общий холестерин", "ммоль/л", 3.00m, 5.20m, 1),
                TestParameter.Create(categoryId, "ЛПНП (липопротеины низкой плотности)", "ммоль/л", 0.00m, 3.00m, 2),
                TestParameter.Create(categoryId, "ЛПВП (липопротеины высокой плотности)", "ммоль/л", 1.00m, 2.00m, 3),
                TestParameter.Create(categoryId, "Триглицериды", "ммоль/л", 0.45m, 1.70m, 4),
                TestParameter.Create(categoryId, "Индекс атерогенности", "ед.", 0.00m, 3.50m, 5)
            };
            await context.TestParameters.AddRangeAsync(parameters);
        }

        private static async Task SeedThyroid(ApplicationDbContext context, int categoryId)
        {
            var parameters = new List<TestParameter>
            {
                TestParameter.Create(categoryId, "ТТГ (тиреотропный гормон)", "мМЕ/л", 0.40m, 4.00m, 1),
                TestParameter.Create(categoryId, "Т4 свободный", "пмоль/л", 9.00m, 19.00m, 2),
                TestParameter.Create(categoryId, "Т3 свободный", "пмоль/л", 2.60m, 5.70m, 3),
                TestParameter.Create(categoryId, "Анти-ТПО", "Ед/мл", 0.00m, 30.00m, 4),
                TestParameter.Create(categoryId, "Анти-ТГ", "Ед/мл", 0.00m, 40.00m, 5)
            };
            await context.TestParameters.AddRangeAsync(parameters);
        }
        private static async Task SeedOAM(ApplicationDbContext context, int categoryId)
        {
            var parameters = new List<TestParameter>
            {
                TestParameter.Create(categoryId, "Удельный вес (относительная плотность)", "ед.", 1.010m, 1.025m, 1),
                TestParameter.Create(categoryId, "pH", "ед.", 5.00m, 7.00m, 2),
                TestParameter.Create(categoryId, "Белок", "г/л", 0.00m, 0.14m, 3),
                TestParameter.Create(categoryId, "Глюкоза", "ммоль/л", 0.00m, 0.10m, 4),
                TestParameter.Create(categoryId, "Лейкоциты", "в п/з", 0.00m, 3.00m, 5),
                TestParameter.Create(categoryId, "Эритроциты", "в п/з", 0.00m, 2.00m, 6),
                TestParameter.Create(categoryId, "Эпителий плоский", "в п/з", 0.00m, 5.00m, 7),
                TestParameter.Create(categoryId, "Бактерии", "в п/з", 0.00m, 0.00m, 8)
            };
            await context.TestParameters.AddRangeAsync(parameters);
        }


    }
    
}
