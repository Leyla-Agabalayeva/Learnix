using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Analytics;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{


    public class AnalyticsService : IAnalyticsService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseReviewRepository _courseReviewRepository;
        private readonly IQuizAttemptRepository _quizAttemptRepository;

        public AnalyticsService(
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseReviewRepository courseReviewRepository,
            IQuizAttemptRepository quizAttemptRepository)
        {
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseReviewRepository = courseReviewRepository;
            _quizAttemptRepository = quizAttemptRepository;
        }

        public async Task<InstructorDashboardStatsDto> GetInstructorDashboardStatsAsync(Guid instructorId, CancellationToken cancellationToken = default)
        {
            var courses = await _courseRepository.GetByInstructorIdAsync(instructorId, cancellationToken);

            if (courses.Count == 0)
            {
                return new InstructorDashboardStatsDto(0, 0, 0, 0, 0);
            }

            var allEnrollments = new List<Enrollment>();
            var allRatings = new List<int>();

            foreach (var course in courses)
            {
                var enrollments = await _enrollmentRepository.GetByCourseIdAsync(course.Id, cancellationToken);
                allEnrollments.AddRange(enrollments.Where(e => e.Status != EnrollmentStatus.Cancelled));

                var reviews = await _courseReviewRepository.GetByCourseIdAsync(course.Id, cancellationToken);
                allRatings.AddRange(reviews.Select(r => r.Rating));
            }

            var totalStudents = allEnrollments.Select(e => e.StudentId).Distinct().Count();
            var averageRating = allRatings.Count > 0 ? Math.Round(allRatings.Average(), 2) : 0;
            var averageCompletionRate = allEnrollments.Count > 0 ? Math.Round(allEnrollments.Average(e => e.ProgressPercentage), 2) : 0;

            return new InstructorDashboardStatsDto(
                courses.Count,
                totalStudents,
                allEnrollments.Count,
                averageRating,
                averageCompletionRate);
        }

        public async Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете смотреть аналитику только своих курсов.");
            }

            var enrollments = (await _enrollmentRepository.GetByCourseIdAsync(courseId, cancellationToken))
                .Where(e => e.Status != EnrollmentStatus.Cancelled)
                .ToList();

            var studentsEnrolled = enrollments.Count;
            var studentsCompleted = enrollments.Count(e => e.Status == EnrollmentStatus.Completed);
            var averageProgress = enrollments.Count > 0 ? Math.Round(enrollments.Average(e => e.ProgressPercentage), 2) : 0;

            var attempts = await _quizAttemptRepository.GetByCourseIdAsync(courseId, cancellationToken);
            var averageQuizScore = attempts.Count > 0 ? Math.Round(attempts.Average(a => a.Percentage), 2) : 0;

            var reviews = await _courseReviewRepository.GetByCourseIdAsync(courseId, cancellationToken);
            var averageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 2) : 0;

            return new CourseAnalyticsDto(
                courseId,
                studentsEnrolled,
                studentsCompleted,
                averageProgress,
                averageQuizScore,
                averageRating,
                reviews.Count);
        }
    }

}
