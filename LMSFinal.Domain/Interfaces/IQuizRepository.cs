using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
    public interface IQuizRepository : IGenericRepository<Quiz>
    {
        Task<Quiz?> GetByLessonIdAsync(Guid lessonId, CancellationToken cancellationToken = default);

        Task<Quiz?> GetWithDetailsAsync(Guid quizId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Quiz>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
    }

}
