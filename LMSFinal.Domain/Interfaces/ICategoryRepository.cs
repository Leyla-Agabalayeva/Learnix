using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{

    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Category>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default);
        Task<Category?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default);
    }


}
