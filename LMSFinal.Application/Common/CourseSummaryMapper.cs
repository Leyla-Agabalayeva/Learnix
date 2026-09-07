using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;

namespace LMSFinal.Application.Common
{
    public static class CourseSummaryMapper
    {
        public static readonly CourseStats EmptyStats = new(0, 0, 0, 0);

        public static CourseSummaryLocalizedDto ToLocalized(
            Course course, LanguageCode language, CourseStats? stats = null)
        {
            var translation = TranslationResolver.Resolve(course.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У курса '{course.Id}' нет ни одного перевода.");

            var courseStats = stats ?? EmptyStats;

            return new CourseSummaryLocalizedDto(
                course.Id,
                course.ThumbnailUrl,
                course.Price,
                course.Level.ToString(),
                course.Status.ToString(),
                translation.LanguageCode.ToString(),
                translation.Title,
                translation.ShortDescription,
                course.Instructor is null
                    ? string.Empty
                    : $"{course.Instructor.FirstName} {course.Instructor.LastName}".Trim(),
                courseStats.AverageRating,
                courseStats.ReviewCount,
                courseStats.EnrollmentCount,
                courseStats.LessonCount,
                course.DurationMinutes);
        }

        public static Task<IReadOnlyDictionary<Guid, CourseStats>> LoadStatsAsync(
            ICourseRepository courseRepository,
            IEnumerable<Course> courses,
            CancellationToken cancellationToken) =>
            courseRepository.GetStatsAsync(
                courses.Select(course => course.Id).Distinct().ToList(),
                cancellationToken);
    }
}
