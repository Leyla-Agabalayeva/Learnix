using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.GradeBook;
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


    public class GradeBookService : IGradeBookService
    {
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;

        public GradeBookService(IQuizAttemptRepository quizAttemptRepository, IEnrollmentRepository enrollmentRepository)
        {
            _quizAttemptRepository = quizAttemptRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<GradeBookDto> GetMyGradeBookAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var attempts = await _quizAttemptRepository.GetGradeBookAsync(studentId, cancellationToken);
            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken);

            // Cancelled-записи не должны портить статистику "средний прогресс по курсам" —
            // студент от них фактически отказался, это не его текущая успеваемость.
            var activeEnrollments = enrollments.Where(e => e.Status != EnrollmentStatus.Cancelled).ToList();

            var rows = attempts
                .Select(a => new GradeBookRowDto(
                    a.Id,
                    a.Quiz.Lesson.Module.CourseId,
                    GetCourseTitle(a.Quiz.Lesson.Module.Course),
                    a.QuizId,
                    GetQuizTitle(a.Quiz),
                    a.Score,
                    a.Percentage,
                    a.Passed,
                    a.CompletedAt))
                .ToList();

            var summary = BuildSummary(attempts, activeEnrollments);

            return new GradeBookDto(summary, rows);
        }

        private static GradeBookSummaryDto BuildSummary(IReadOnlyList<QuizAttempt> attempts, IReadOnlyList<Enrollment> activeEnrollments)
        {
            var averageQuizScore = attempts.Count > 0
                ? Math.Round(attempts.Average(a => a.Percentage), 2)
                : 0;

            // По РАЗНЫМ квизам (distinct QuizId), а не по количеству попыток — иначе пересдача
            // одного и того же квиза 3 раза задваивала бы "Completed Quizzes".
            var quizGroups = attempts.GroupBy(a => a.QuizId).ToList();
            var completedQuizzes = quizGroups.Count;
            var passedQuizzes = quizGroups.Count(g => g.Any(a => a.Passed));

            var averageCourseCompletion = activeEnrollments.Count > 0
                ? Math.Round(activeEnrollments.Average(e => e.ProgressPercentage), 2)
                : 0;

            var completedCoursesCount = activeEnrollments.Count(e => e.Status == EnrollmentStatus.Completed);

            return new GradeBookSummaryDto(
                averageQuizScore,
                completedQuizzes,
                passedQuizzes,
                averageCourseCompletion,
                activeEnrollments.Count,
                completedCoursesCount);
        }

        private static string GetCourseTitle(Course course)
        {
            var translation = course.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)
                ?? course.Translations.FirstOrDefault();

            return translation?.Title ?? "—";
        }

        private static string GetQuizTitle(Quiz quiz)
        {
            var translation = quiz.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)
                ?? quiz.Translations.FirstOrDefault();

            return translation?.Title ?? "—";
        }
    }

}
