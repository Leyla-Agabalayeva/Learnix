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
    public class LessonProgressRepository : BaseRepository<LessonProgress>, ILessonProgressRepository
    {
        public LessonProgressRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<LessonProgress?> GetAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default) =>
            await Context.LessonProgresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lessonId, cancellationToken);

        public async Task<IReadOnlyList<LessonProgress>> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.LessonProgresses
                .Where(p => p.StudentId == studentId && p.Lesson.Module.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<Lesson?> GetNextIncompleteLessonAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var lessons = await Context.Lessons
                .Include(l => l.Module)
                .Include(l => l.Translations)
                .Where(l => l.Module.CourseId == courseId && l.IsPublished)
                .OrderBy(l => l.Module.OrderIndex)
                .ThenBy(l => l.OrderIndex)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var completedLessonIds = await Context.LessonProgresses
                .Where(p => p.StudentId == studentId && p.Lesson.Module.CourseId == courseId && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync(cancellationToken);

            return lessons.FirstOrDefault(l => !completedLessonIds.Contains(l.Id));
        }
    }

}
