using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Progress;
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

    public class ProgressService : IProgressService
    {
        private readonly ILessonProgressRepository _lessonProgressRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICertificateService _certificateService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public ProgressService(
            ILessonProgressRepository lessonProgressRepository,
            ILessonRepository lessonRepository,
            IEnrollmentRepository enrollmentRepository,
            ICertificateService certificateService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _lessonProgressRepository = lessonProgressRepository;
            _lessonRepository = lessonRepository;
            _enrollmentRepository = enrollmentRepository;
            _certificateService = certificateService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<LessonProgressDto> MarkCompleteAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            var courseId = lesson.Module.CourseId;

            var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(studentId, courseId, cancellationToken);
            if (!isEnrolled)
            {
                throw new ForbiddenAccessException("Чтобы отмечать уроки пройденными, нужно быть записанным на курс.");
            }

            var progress = await _lessonProgressRepository.GetAsync(studentId, lessonId, cancellationToken);

            if (progress is null)
            {
                progress = new LessonProgress
                {
                    StudentId = studentId,
                    LessonId = lessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };

                await _lessonProgressRepository.AddAsync(progress, cancellationToken);
            }
            else
            {
                progress.IsCompleted = true;
                progress.CompletedAt ??= DateTime.UtcNow; // не затираем дату первого прохождения при повторном вызове
                progress.LastAccessedAt = DateTime.UtcNow;
                _lessonProgressRepository.Update(progress);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Пересчитываем Enrollment.ProgressPercentage сразу же : "Backend автоматически
            // пересчитывает общий progress курса" после каждого mark-complete, а не по расписанию.
            await RecalculateEnrollmentProgressAsync(studentId, courseId, cancellationToken);

            return new LessonProgressDto(progress.Id, progress.LessonId, progress.IsCompleted, progress.CompletedAt, progress.LastPosition, progress.LastAccessedAt);
        }

        public async Task<CourseProgressDto> GetCourseProgressAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);
            if (enrollment is null || enrollment.Status == EnrollmentStatus.Cancelled)
            {
                throw new ForbiddenAccessException("Вы не записаны на этот курс.");
            }

            var snapshot = await CalculateProgressAsync(studentId, courseId, cancellationToken);

            return new CourseProgressDto(
                courseId,
                snapshot.Total,
                snapshot.Completed,
                snapshot.Percentage,
                enrollment.Status == EnrollmentStatus.Completed,
                snapshot.CompletedLessonIds);
        }

        public async Task<ContinueLearningDto> GetContinueLearningAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);
            if (enrollment is null || enrollment.Status == EnrollmentStatus.Cancelled)
            {
                throw new ForbiddenAccessException("Вы не записаны на этот курс.");
            }

            var nextLesson = await _lessonProgressRepository.GetNextIncompleteLessonAsync(studentId, courseId, cancellationToken);

            if (nextLesson is null)
            {
                // Все опубликованные уроки пройдены — фронтенду показывать нечего, вести на сертификат/отзыв.
                return new ContinueLearningDto(null, null, null, CourseCompleted: true);
            }

            return new ContinueLearningDto(nextLesson.Id, nextLesson.ModuleId, GetDisplayTitle(nextLesson), CourseCompleted: false);
        }

        // --- helpers ---

        private async Task<ProgressSnapshot> CalculateProgressAsync(
            Guid studentId, Guid courseId, CancellationToken cancellationToken)
        {
            var allLessons = await _lessonRepository.GetByCourseIdAsync(courseId, cancellationToken);
            var publishedLessonIds = allLessons.Where(l => l.IsPublished).Select(l => l.Id).ToHashSet();
            var totalLessons = publishedLessonIds.Count;

            if (totalLessons == 0)
            {
                return new ProgressSnapshot(0, 0, 0, Array.Empty<Guid>());
            }

            var progressRecords = await _lessonProgressRepository.GetByStudentAndCourseAsync(studentId, courseId, cancellationToken);

            var completedLessonIds = progressRecords
                .Where(p => p.IsCompleted && publishedLessonIds.Contains(p.LessonId))
                .Select(p => p.LessonId)
                .ToList();

            var percentage = Math.Round(completedLessonIds.Count * 100.0 / totalLessons, 2);

            return new ProgressSnapshot(totalLessons, completedLessonIds.Count, percentage, completedLessonIds);
        }

        private async Task RecalculateEnrollmentProgressAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken)
        {
            var snapshot = await CalculateProgressAsync(studentId, courseId, cancellationToken);

            var enrollment = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);
            if (enrollment is null)
            {
                return; // защитный случай — MarkCompleteAsync уже проверил Enrollment выше по стеку
            }

            enrollment.ProgressPercentage = snapshot.Percentage;

            var justCompleted = snapshot.Total > 0 && snapshot.Completed == snapshot.Total && enrollment.Status == EnrollmentStatus.Active;

            if (justCompleted)
            {
                enrollment.Status = EnrollmentStatus.Completed;
                enrollment.CompletedAt = DateTime.UtcNow;
            }

            _enrollmentRepository.Update(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (justCompleted)
            {
                // Событие "course completed" .
                await _notificationService.NotifyAsync(
                    studentId,
                    "Course completed 🎉",
                    "You've completed all lessons in this course. Check if you're eligible for a certificate!",
                    NotificationType.CourseCompleted,
                    cancellationToken);
            }

            //  попытка выдать сертификат при каждом mark-complete — идемпотентно
            // (IssueIfEligibleAsync сам проверит "уже выдан?" и молча ничего не сделает).
            await _certificateService.IssueIfEligibleAsync(studentId, courseId, cancellationToken);
        }

        private sealed record ProgressSnapshot(
            int Total,
            int Completed,
            double Percentage,
            IReadOnlyList<Guid> CompletedLessonIds);

        private static string? GetDisplayTitle(Lesson lesson)
        {
            var translation = lesson.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)
                ?? lesson.Translations.FirstOrDefault();

            return translation?.Title;
        }
    }

}
