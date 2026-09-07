using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
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
    public class CourseRepository : BaseRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Course?> GetWithDetailsAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Courses
                .Include(c => c.Translations)
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        public async Task<Course?> GetWithCurriculumAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Courses
                .Include(c => c.Translations)
                .Include(c => c.Category)
                    .ThenInclude(category => category.Translations)
                .Include(c => c.Instructor)
                .Include(c => c.Modules)
                    .ThenInclude(module => module.Translations)
                .Include(c => c.Modules)
                    .ThenInclude(module => module.Lessons)
                        .ThenInclude(lesson => lesson.Translations)
                .Include(c => c.Modules)
                    .ThenInclude(module => module.Lessons)
                        .ThenInclude(lesson => lesson.Quiz)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        public async Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
            string? searchTerm,
            Guid? categoryId,
            CourseLevel? level,
            decimal? minPrice,
            decimal? maxPrice,
            CourseStatus? status,
            CourseSortBy sortBy,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = Context.Courses
                .Include(c => c.Translations)
                .Include(c => c.Category)
                .Include(c => c.Instructor)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            if (level.HasValue)
            {
                query = query.Where(c => c.Level == level.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(c => c.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c => c.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.Translations.Any(t =>
                    t.Title.Contains(searchTerm) || t.ShortDescription.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await ApplySort(query, sortBy)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

  
        private static IQueryable<Course> ApplySort(IQueryable<Course> query, CourseSortBy sortBy) => sortBy switch
        {
            CourseSortBy.Popular => query
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenByDescending(c => c.CreatedAt),

            CourseSortBy.Rating => query
                // Курс без отзывов не должен обгонять курс с оценками, поэтому пустое
                // среднее приводится к нулю, а не к NULL.
                .OrderByDescending(c => c.Reviews.Any() ? c.Reviews.Average(r => (double)r.Rating) : 0)
                .ThenByDescending(c => c.Reviews.Count)
                .ThenByDescending(c => c.CreatedAt),

            CourseSortBy.PriceAsc => query
                .OrderBy(c => c.Price)
                .ThenByDescending(c => c.CreatedAt),

            CourseSortBy.PriceDesc => query
                .OrderByDescending(c => c.Price)
                .ThenByDescending(c => c.CreatedAt),

            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        public async Task<IReadOnlyList<Course>> GetByInstructorIdAsync(Guid instructorId, CancellationToken cancellationToken = default) =>
            await Context.Courses
                .Include(c => c.Translations)
                .Where(c => c.InstructorId == instructorId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Courses.AnyAsync(c => c.Id == courseId, cancellationToken);

        public async Task<IReadOnlyDictionary<Guid, CourseStats>> GetStatsAsync(
            IReadOnlyCollection<Guid> courseIds,
            CancellationToken cancellationToken = default)
        {
            if (courseIds.Count == 0)
            {
                return new Dictionary<Guid, CourseStats>();
            }

            var ratings = await Context.CourseReviews
                .Where(review => courseIds.Contains(review.CourseId))
                .GroupBy(review => review.CourseId)
                .Select(group => new
                {
                    CourseId = group.Key,
                    Average = group.Average(review => (double)review.Rating),
                    Count = group.Count()
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var enrollments = await Context.Enrollments
                .Where(enrollment => courseIds.Contains(enrollment.CourseId))
                .GroupBy(enrollment => enrollment.CourseId)
                .Select(group => new { CourseId = group.Key, Count = group.Count() })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var lessons = await Context.Lessons
                .Where(lesson => lesson.IsPublished && courseIds.Contains(lesson.Module.CourseId))
                .GroupBy(lesson => lesson.Module.CourseId)
                .Select(group => new { CourseId = group.Key, Count = group.Count() })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var ratingByCourse = ratings.ToDictionary(item => item.CourseId);
            var enrollmentByCourse = enrollments.ToDictionary(item => item.CourseId, item => item.Count);
            var lessonByCourse = lessons.ToDictionary(item => item.CourseId, item => item.Count);

            return courseIds.ToDictionary(
                courseId => courseId,
                courseId => new CourseStats(
                    ratingByCourse.TryGetValue(courseId, out var rating) ? Math.Round(rating.Average, 1) : 0,
                    ratingByCourse.TryGetValue(courseId, out var reviews) ? reviews.Count : 0,
                    enrollmentByCourse.GetValueOrDefault(courseId),
                    lessonByCourse.GetValueOrDefault(courseId)));
        }
    }

}
