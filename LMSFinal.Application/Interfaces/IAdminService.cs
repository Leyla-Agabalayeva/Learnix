using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Admin;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Application.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<AdminUserDto>> SearchUsersAsync(AdminUserSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>Блокирует/разблокирует вход. Себя заблокировать нельзя.</summary>
        Task SetUserLockAsync(Guid currentAdminId, Guid userId, bool locked, CancellationToken cancellationToken = default);

        /// <summary>Себя и последнего оставшегося админа удалить нельзя.</summary>
        Task DeleteUserAsync(Guid currentAdminId, Guid userId, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminCourseDto>> SearchCoursesAsync(AdminCourseSearchRequest request, CancellationToken cancellationToken = default);

        Task SetCourseStatusAsync(Guid courseId, CourseStatus status, CancellationToken cancellationToken = default);

        Task SetCourseThumbnailAsync(Guid courseId, string thumbnailUrl, CancellationToken cancellationToken = default);

        Task DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    }
}
