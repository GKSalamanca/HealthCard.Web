using System;
using System.Collections.Generic;
using System.Text;
using Models.ParameterBase;

namespace Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();
    }
}
