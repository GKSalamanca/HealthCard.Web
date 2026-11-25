using Data;
using HealthCard.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models.ParameterBase;
using Services.Interfaces;

namespace HealthCard.Web.Services;

public class RecordQueryService(ApplicationDbContext db, IResultEvaluationService evaluation)
{
    public IQueryable<Category> Categories(string userId) => db.Categories.AsNoTracking().Where(c => c.IsSystem || c.UserId == userId);
    public async Task<List<ResultRowViewModel>> ResultsAsync(string userId, int? categoryId = null, DateOnly? fromDate = null, DateOnly? toDate = null, int? recordId = null)
    {
        var query = db.TestResults.AsNoTracking().Where(r => r.MedicalRecord.UserId == userId);
        if (categoryId.HasValue) query = query.Where(r => r.MedicalRecord.CategoryId == categoryId);
        if (recordId.HasValue) query = query.Where(r => r.MedicalRecordId == recordId);
        if (fromDate.HasValue) query = query.Where(r => r.MedicalRecord.RecordDate >= fromDate);
        if (toDate.HasValue) query = query.Where(r => r.MedicalRecord.RecordDate <= toDate);
        var norms = await db.UserParameterNorms.AsNoTracking().Where(n => n.UserId == userId).ToDictionaryAsync(n => n.TestParameterId);
        var rows = await query.OrderBy(r => r.MedicalRecord.RecordDate).ThenBy(r => r.MedicalRecordId).ThenBy(r => r.TestParameter.SortOrder)
            .Select(r => new ResultRowViewModel
            {
                RecordId = r.MedicalRecordId,
                Date = r.MedicalRecord.RecordDate,
                Title = r.MedicalRecord.Title,
                CategoryId = r.MedicalRecord.CategoryId,
                CategoryName = r.MedicalRecord.Category.Name,
                ParameterId = r.TestParameterId,
                ParameterName = r.TestParameter.Name,
                Unit = r.TestParameter.Unit,
                Value = r.Value,
                NormMin = r.TestParameter.NormMin,
                NormMax = r.TestParameter.NormMax
            }).ToListAsync();
        foreach (var row in rows)
        {
            if (norms.TryGetValue(row.ParameterId, out var norm))
            { row.NormMin = norm.CustomNormMin; row.NormMax = norm.CustomNormMax; row.IsCustomNorm = true; }
            row.Status = evaluation.Evaluate(row.Value, row.NormMin, row.NormMax);
        }
        return rows;
    }
}
