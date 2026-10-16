using Services.Interfaces;
using Data;
using Models.ParameterBase;
using SQLitePCL;
using Microsoft.EntityFrameworkCore;

namespace Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync() 
        {
            return await _context.Categories
                .Where(c => c.IsSystem)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }
    }
}
