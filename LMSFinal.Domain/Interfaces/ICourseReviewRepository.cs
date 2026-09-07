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

        Task<double> GetAverageRatingAsync(Guid courseId, CancellationToken cancellationToken = default);
    }
}
