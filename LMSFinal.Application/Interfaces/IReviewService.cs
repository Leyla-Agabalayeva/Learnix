using LMSFinal.Contracts.DTOs.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Domain.Enums;

namespace LMSFinal.Application.Interfaces
{

    public interface IReviewService
    {

        Task<IReadOnlyList<CourseReviewDto>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    
        Task<IReadOnlyList<InstructorReviewDto>> GetInstructorReviewsAsync(
            Guid instructorId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<CourseReviewDto> CreateAsync(Guid studentId, Guid courseId, CreateReviewRequest request, CancellationToken cancellationToken = default);

        Task<CourseReviewDto> UpdateAsync(Guid studentId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid studentId, Guid reviewId, CancellationToken cancellationToken = default);

        Task<InstructorReviewDto> ReplyAsync(
            Guid instructorId, Guid reviewId, LanguageCode language, ReplyToReviewRequest request, CancellationToken cancellationToken = default);
    }

}
