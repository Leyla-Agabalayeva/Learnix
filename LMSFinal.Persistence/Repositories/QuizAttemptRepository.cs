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

    public class QuizAttemptRepository : BaseRepository<QuizAttempt>, IQuizAttemptRepository
    {
        public QuizAttemptRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<QuizAttempt>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            await Context.QuizAttempts
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.CompletedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<QuizAttempt>> GetByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default) =>
            await Context.QuizAttempts
                .Include(a => a.Student)
                .Where(a => a.QuizId == quizId)
                .OrderByDescending(a => a.CompletedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        public async Task<IReadOnlyList<QuizAttempt>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.QuizAttempts
                .Where(a => a.Quiz.Lesson.Module.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<bool> HasAttemptsAsync(Guid quizId, CancellationToken cancellationToken = default) =>
            await Context.QuizAttempts.AnyAsync(a => a.QuizId == quizId, cancellationToken);

        public async Task<IReadOnlyList<QuizAttempt>> GetGradeBookAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            await Context.QuizAttempts
                .Include(a => a.Quiz).ThenInclude(q => q.Translations)
                .Include(a => a.Quiz).ThenInclude(q => q.Lesson).ThenInclude(l => l.Module).ThenInclude(m => m.Course)
                    .ThenInclude(c => c.Translations)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.CompletedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }

}