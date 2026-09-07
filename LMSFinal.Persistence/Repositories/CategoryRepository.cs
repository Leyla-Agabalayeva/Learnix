using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            await Context.Categories
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        public async Task<IReadOnlyList<Category>> GetAllWithTranslationsAsync(CancellationToken cancellationToken = default) =>
            await Context.Categories
                .Include(c => c.Translations)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<Category?> GetByIdWithTranslationsAsync(Guid id, CancellationToken cancellationToken = default) =>
            await Context.Categories
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

}
