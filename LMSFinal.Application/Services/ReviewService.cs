using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Reviews;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Application.Common;

namespace LMSFinal.Application.Services
{

    public class ReviewService : IReviewService
    {
        private readonly ICourseReviewRepository _courseReviewRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(
            ICourseReviewRepository courseReviewRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork)
        {
            _courseReviewRepository = courseReviewRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<CourseReviewDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            var reviews = await _courseReviewRepository.GetByCourseIdAsync(courseId, cancellationToken);
            return reviews.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<InstructorReviewDto>> GetInstructorReviewsAsync(
            Guid instructorId, LanguageCode language, CancellationToken cancellationToken = default)
        {

            var courses = await _courseRepository.GetByInstructorIdAsync(instructorId, cancellationToken);

            if (courses.Count == 0)
            {
                return Array.Empty<InstructorReviewDto>();
            }

            var reviews = await _courseReviewRepository.GetByCourseIdsAsync(
                courses.Select(course => course.Id).ToList(), cancellationToken);

            return reviews
                .Select(review => new InstructorReviewDto(
                    review.Id,
                    review.CourseId,
                    TranslationResolver
                        .Resolve(review.Course.Translations, language, t => t.LanguageCode)?.Title ?? "—",
                    review.StudentId,
                    $"{review.Student.FirstName} {review.Student.LastName}".Trim(),
                    review.Rating,
                    review.Comment,
                    review.CreatedAt,
                    review.UpdatedAt))
                .ToList();
        }

        public async Task<CourseReviewDto> CreateAsync(Guid studentId, Guid courseId, CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            var enrollment = await _enrollmentRepository.GetAsync(studentId, courseId, cancellationToken);
            if (enrollment is null || enrollment.Status == EnrollmentStatus.Cancelled)
            {
                throw new ForbiddenAccessException("Оставить отзыв может только студент, записанный на этот курс.");
            }

            var existing = await _courseReviewRepository.GetByStudentAndCourseAsync(studentId, courseId, cancellationToken);
            if (existing is not null)
            {
                throw new ConflictException("Вы уже оставили отзыв на этот курс. Отредактируйте существующий вместо создания нового.");
            }

            var review = new CourseReview
            {
                CourseId = courseId,
                StudentId = studentId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            await _courseReviewRepository.AddAsync(review, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _courseReviewRepository.GetByStudentAndCourseAsync(studentId, courseId, cancellationToken)
                ?? throw new NotFoundException("Review", review.Id);

            return MapToDto(created);
        }

        public async Task<CourseReviewDto> UpdateAsync(Guid studentId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default)
        {
            var review = await _courseReviewRepository.GetByIdAsync(reviewId, cancellationToken)
                ?? throw new NotFoundException("Review", reviewId);

            EnsureOwnership(review, studentId);

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            review.UpdatedAt = DateTime.UtcNow;

            _courseReviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updated = await _courseReviewRepository.GetByStudentAndCourseAsync(studentId, review.CourseId, cancellationToken)
                ?? throw new NotFoundException("Review", reviewId);

            return MapToDto(updated);
        }

        public async Task DeleteAsync(Guid studentId, Guid reviewId, CancellationToken cancellationToken = default)
        {
            var review = await _courseReviewRepository.GetByIdAsync(reviewId, cancellationToken)
                ?? throw new NotFoundException("Review", reviewId);

            EnsureOwnership(review, studentId);

            _courseReviewRepository.Remove(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // --- helpers ---

        private static void EnsureOwnership(CourseReview review, Guid studentId)
        {
            if (review.StudentId != studentId)
            {
                throw new ForbiddenAccessException("Вы можете редактировать и удалять только свои отзывы.");
            }
        }

        private static CourseReviewDto MapToDto(CourseReview review) => new(
            review.Id,
            review.CourseId,
            review.StudentId,
            $"{review.Student.FirstName} {review.Student.LastName}",
            review.Rating,
            review.Comment,
            review.CreatedAt,
            review.UpdatedAt);
    }

}
