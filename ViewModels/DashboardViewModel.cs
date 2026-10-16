using Models.ParameterBase;

namespace HealthCard.Web.ViewModels;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public int TotalRecordsCount { get; set; }
    public int OutOfRangeCount { get; set; }

    // Список последних записей для отображения
    public List<MedicalRecord> RecentRecords { get; set; } = new();

    // Список категорий для быстрого перехода
    public List<Category> Categories { get; set; } = new();

    //Статистика по каждой категории
    public List<CategoryStatisticsViewModel> CategoryStatistics { get; set; } = new();
}

public class CategoryStatisticsViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsLabTest { get; set; }
    public string? IconClass { get; set; }
    public int RecordsCount { get; set; }
    public DateOnly? LastRecordDate { get; set; }
}