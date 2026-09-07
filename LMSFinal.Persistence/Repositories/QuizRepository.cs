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

    public class QuizRepository : BaseRepository<Quiz>, IQuizRepository
    {
        public QuizRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Quiz?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
            await Context.Quizzes.FirstOrDefaultAsync(q => q.LessonId == lessonId, cancellationToken);

        public async Task<Quiz?> GetWithDetailsAsync(Guid quizId, CancellationToken cancellationToken = default) =>
            await Context.Quizzes
                .Include(q => q.Lesson).ThenInclude(l => l.Module).ThenInclude(m => m.Course)
                .Include(q => q.Translations)
                .Include(q => q.Questions.OrderBy(question => question.OrderIndex))
                    .ThenInclude(question => question.Translations)
                .Include(q => q.Questions)
                    .ThenInclude(question => question.Answers)
                        .ThenInclude(answer => answer.Translations)
                .AsSplitQuery()
                .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
        public async Task<IReadOnlyList<Quiz>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Quizzes
                .Where(q => q.Lesson.Module.CourseId == courseId && q.Lesson.IsPublished)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }

}
