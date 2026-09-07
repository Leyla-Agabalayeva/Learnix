using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
    public sealed record CourseStats(
        double AverageRating,
        int ReviewCount,
        int EnrollmentCount,
        int LessonCount);

    public interface ICourseRepository : IGenericRepository<Course>
    {

        Task<Course?> GetWithDetailsAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<Course?> GetWithCurriculumAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
            string? searchTerm,
            Guid? categoryId,
            CourseLevel? level,
            decimal? minPrice,
            decimal? maxPrice,
            CourseStatus? status,
            CourseSortBy sortBy,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Course>> GetByInstructorIdAsync(Guid instructorId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid courseId, CancellationToken cancellationToken = default);


        Task<IReadOnlyDictionary<Guid, CourseStats>> GetStatsAsync(
            IReadOnlyCollection<Guid> courseIds,
            CancellationToken cancellationToken = default);
    }

}
