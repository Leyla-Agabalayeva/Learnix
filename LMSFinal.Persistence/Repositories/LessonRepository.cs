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


    public class LessonRepository : BaseRepository<Lesson>, ILessonRepository
    {
        public LessonRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Lesson>> GetByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default) =>
            await Context.Lessons
                .Include(l => l.Translations)
                .Include(l => l.Resources)
                .Include(l => l.Quiz)
                .Where(l => l.ModuleId == moduleId)
                .OrderBy(l => l.OrderIndex)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        public async Task<IReadOnlyList<Lesson>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Lessons
                .Where(l => l.Module.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<Lesson?> GetWithModuleAndCourseAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
            await Context.Lessons
                .Include(l => l.Translations)
                .Include(l => l.Module).ThenInclude(m => m.Course)
                .Include(l => l.Resources)
                .Include(l => l.Quiz)
                .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        public async Task<IReadOnlyList<Lesson>> GetByIdsWithModuleAndCourseAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            await Context.Lessons
                .Include(l => l.Module).ThenInclude(m => m.Course)
                .Where(l => ids.Contains(l.Id))
                .ToListAsync(cancellationToken);

        public async Task<int> GetNextOrderIndexAsync(Guid moduleId, CancellationToken cancellationToken = default)
        {
            var maxOrderIndex = await Context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .Select(l => (int?)l.OrderIndex)
                .MaxAsync(cancellationToken);

            return (maxOrderIndex ?? -1) + 1;
        }
    }


}
