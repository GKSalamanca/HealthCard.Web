namespace HealthCard.Web.ViewModels;

public class CategoryStatsViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int RecordsCount { get; set; }
    public DateOnly? LastRecordDate { get; set; }
}