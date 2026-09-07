using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface ICourseService
    {
        Task<PagedResult<CourseSummaryDto>> SearchAsync(CourseSearchRequest request, CancellationToken cancellationToken = default);

        
        Task<CourseDto> GetByIdAsync(Guid courseId, Guid? currentUserId, CancellationToken cancellationToken = default);

        Task<PagedResult<CourseSummaryLocalizedDto>> SearchLocalizedAsync(CourseSearchRequest request, LanguageCode language, CancellationToken cancellationToken = default);

        Task<CourseLocalizedDto> GetByIdLocalizedAsync(Guid courseId, Guid? currentUserId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CourseSummaryDto>> GetMyCoursesAsync(Guid instructorId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CourseSummaryLocalizedDto>> GetMyCoursesLocalizedAsync(
            Guid instructorId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<CourseDto> CreateAsync(Guid instructorId, CreateCourseRequest request, CancellationToken cancellationToken = default);

        Task<CourseDto> UpdateAsync(Guid instructorId, Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);

        Task<CourseDto> PublishAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);

        Task<CourseDto> UnpublishAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);

        Task<CourseDto> ArchiveAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);
    }

}
