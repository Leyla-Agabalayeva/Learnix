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
    public class CourseReviewRepository : BaseRepository<CourseReview>, ICourseReviewRepository
    {
        public CourseReviewRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<CourseReview>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.CourseReviews
                .Include(r => r.Student)
                .Where(r => r.CourseId == courseId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<CourseReview>> GetByCourseIdsAsync(
            IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default)
        {
            if (courseIds.Count == 0)
            {
                return Array.Empty<CourseReview>();
            }

            return await Context.CourseReviews
                .Include(r => r.Student)
                .Include(r => r.Course).ThenInclude(c => c.Translations)
                .Where(r => courseIds.Contains(r.CourseId))
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<CourseReview?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.CourseReviews
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.CourseId == courseId, cancellationToken);

        public async Task<double> GetAverageRatingAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            var ratings = await Context.CourseReviews
                .Where(r => r.CourseId == courseId)
                .Select(r => r.Rating)
                .ToListAsync(cancellationToken);

            return ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 2);
        }
    }

}
