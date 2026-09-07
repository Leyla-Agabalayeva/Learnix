namespace LMSFinal.Contracts.DTOs.Courses
{

    public record CourseCurriculumModuleDto(
        Guid Id,
        int OrderIndex,
        string Title,
        int LessonCount,
        int DurationMinutes,
        IReadOnlyList<CourseCurriculumLessonDto> Lessons);

    public record CourseCurriculumLessonDto(
        Guid Id,
        int OrderIndex,
        string Title,
        int DurationMinutes,
        bool HasQuiz);
}
