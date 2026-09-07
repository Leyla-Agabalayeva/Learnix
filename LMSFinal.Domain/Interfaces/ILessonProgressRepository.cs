using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
    public interface ILessonProgressRepository : IGenericRepository<LessonProgress>
    {
        Task<LessonProgress?> GetAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LessonProgress>> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<Lesson?> GetNextIncompleteLessonAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    }

}
