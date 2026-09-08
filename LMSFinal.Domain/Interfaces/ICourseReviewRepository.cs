using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
   
    public interface ICourseReviewRepository : IGenericRepository<CourseReview>
    {
        Task<IReadOnlyList<CourseReview>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CourseReview>> GetByCourseIdsAsync(
            IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default);

        Task<CourseReview?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        /// <summary>Отзыв с загруженным курсом (переводы + InstructorId) — для проверки владения курсом при ответе преподавателя.</summary>
        Task<CourseReview?> GetByIdWithCourseAsync(Guid id, CancellationToken cancellationToken = default);

        Task<double> GetAverageRatingAsync(Guid courseId, CancellationToken cancellationToken = default);
    }
}
