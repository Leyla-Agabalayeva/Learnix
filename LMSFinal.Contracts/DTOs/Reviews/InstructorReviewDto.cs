namespace LMSFinal.Contracts.DTOs.Reviews
{

    public record InstructorReviewDto(
        Guid Id,
        Guid CourseId,
        string CourseTitle,
        Guid StudentId,
        string StudentName,
        int Rating,
        string? Comment,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
