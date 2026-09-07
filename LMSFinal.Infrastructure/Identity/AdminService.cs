using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Admin;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMSFinal.Infrastructure.Identity
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICourseRepository _courseRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            ICourseRepository courseRepository,
            ICategoryRepository categoryRepository,
            IEnrollmentRepository enrollmentRepository,
            ICertificateRepository certificateRepository,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _courseRepository = courseRepository;
            _categoryRepository = categoryRepository;
            _enrollmentRepository = enrollmentRepository;
            _certificateRepository = certificateRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _userManager.Users.CountAsync(cancellationToken);
            var totalStudents = (await _userManager.GetUsersInRoleAsync("Student")).Count;
            var totalInstructors = (await _userManager.GetUsersInRoleAsync("Instructor")).Count;
            var totalAdmins = (await _userManager.GetUsersInRoleAsync("Admin")).Count;

            var courses = await _courseRepository.GetAllAsync(cancellationToken);
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var enrollments = await _enrollmentRepository.GetAllAsync(cancellationToken);
            var certificates = await _certificateRepository.GetAllAsync(cancellationToken);

            return new AdminDashboardStatsDto(
                totalUsers, totalStudents, totalInstructors, totalAdmins,
                courses.Count,
                courses.Count(c => c.Status == CourseStatus.Published),
                courses.Count(c => c.Status == CourseStatus.Draft),
                courses.Count(c => c.Status == CourseStatus.Archived),
                categories.Count,
                enrollments.Count,
                certificates.Count,
                await GetSignupsLast30DaysAsync(cancellationToken));
        }

        private async Task<IReadOnlyList<DailyCountDto>> GetSignupsLast30DaysAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var since = today.AddDays(-29);

            var recentUsers = await _userManager.Users
                .Where(u => u.CreatedAt >= since)
                .Select(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            var byDate = recentUsers
                .GroupBy(createdAt => createdAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var series = new List<DailyCountDto>(30);
            for (var day = since; day <= today; day = day.AddDays(1))
            {
                byDate.TryGetValue(day, out var count);
                series.Add(new DailyCountDto(day.ToString("yyyy-MM-dd"), count));
            }

            return series;
        }

        public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(
            AdminUserSearchRequest request, CancellationToken cancellationToken = default)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(u =>
                    u.Email!.Contains(term) || u.FirstName.Contains(term) || u.LastName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var idsInRole = (await _userManager.GetUsersInRoleAsync(request.Role))
                    .Select(u => u.Id)
                    .ToList();
                query = query.Where(u => idsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var items = new List<AdminUserDto>(users.Count);
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new AdminUserDto(
                    user.Id, user.Email!, user.FirstName, user.LastName,
                    roles.ToList(), await _userManager.IsLockedOutAsync(user), user.CreatedAt, user.AvatarUrl));
            }

            return new PagedResult<AdminUserDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
        }

        public async Task SetUserLockAsync(Guid currentAdminId, Guid userId, bool locked, CancellationToken cancellationToken = default)
        {
            if (userId == currentAdminId)
            {
                throw new ConflictException("Нельзя заблокировать самого себя.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User", userId);

            await _userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);
        }

        public async Task DeleteUserAsync(Guid currentAdminId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == currentAdminId)
            {
                throw new ConflictException("Нельзя удалить самого себя.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User", userId);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var remainingAdmins = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
                if (remainingAdmins <= 1)
                {
                    throw new ConflictException("Нельзя удалить последнего администратора.");
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new ConflictException(string.Join(" ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task<PagedResult<AdminCourseDto>> SearchCoursesAsync(
            AdminCourseSearchRequest request, CancellationToken cancellationToken = default)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            CourseStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<CourseStatus>(request.Status, true, out var parsed))
            {
                status = parsed;
            }

            var (courses, totalCount) = await _courseRepository.SearchAsync(
                request.SearchTerm, categoryId: null, level: null, minPrice: null, maxPrice: null,
                status, CourseSortBy.Newest, page, pageSize, cancellationToken);

            var stats = await _courseRepository.GetStatsAsync(courses.Select(c => c.Id).ToList(), cancellationToken);

            var items = courses.Select(course =>
            {
                var translation = course.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)
                    ?? course.Translations.FirstOrDefault();

                stats.TryGetValue(course.Id, out var courseStats);

                return new AdminCourseDto(
                    course.Id,
                    translation?.Title ?? "—",
                    course.ThumbnailUrl,
                    $"{course.Instructor.FirstName} {course.Instructor.LastName}",
                    course.Category.Slug,
                    course.Status.ToString(),
                    courseStats?.EnrollmentCount ?? 0,
                    course.CreatedAt);
            }).ToList();

            return new PagedResult<AdminCourseDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
        }

        public async Task SetCourseStatusAsync(Guid courseId, CourseStatus status, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            course.Status = status;
            course.UpdatedAt = DateTime.UtcNow;

            _courseRepository.Update(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task SetCourseThumbnailAsync(Guid courseId, string thumbnailUrl, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            course.ThumbnailUrl = thumbnailUrl;
            course.UpdatedAt = DateTime.UtcNow;

            _courseRepository.Update(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            _courseRepository.Remove(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
