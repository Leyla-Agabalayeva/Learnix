using System;

namespace LMSFinal.Contracts.DTOs.Admin
{
    public record AdminCourseDto(
        Guid Id,
        string Title,
        string? ThumbnailUrl,
        string InstructorName,
        string CategoryName,
        string Status,
        int EnrollmentCount,
        DateTime CreatedAt);
}
