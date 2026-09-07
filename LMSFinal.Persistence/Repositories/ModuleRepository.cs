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
    public class ModuleRepository : BaseRepository<Module>, IModuleRepository
    {
        public ModuleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Module>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Modules
                .Include(m => m.Translations)
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.OrderIndex)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<Module?> GetWithCourseAsync(Guid moduleId, CancellationToken cancellationToken = default) =>
            await Context.Modules
                .Include(m => m.Translations)
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        public async Task<IReadOnlyList<Module>> GetByIdsWithCourseAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            await Context.Modules
                .Include(m => m.Course)
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(cancellationToken);

        public async Task<int> GetNextOrderIndexAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            var maxOrderIndex = await Context.Modules
                .Where(m => m.CourseId == courseId)
                .Select(m => (int?)m.OrderIndex)
                .MaxAsync(cancellationToken);

            return (maxOrderIndex ?? -1) + 1;
        }
    }

}
