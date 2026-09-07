using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{

    public interface IQuizAttemptRepository : IGenericRepository<QuizAttempt>
    {
        Task<IReadOnlyList<QuizAttempt>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuizAttempt>> GetByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuizAttempt>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<bool> HasAttemptsAsync(Guid quizId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuizAttempt>> GetGradeBookAsync(Guid studentId, CancellationToken cancellationToken = default);
    }


}
