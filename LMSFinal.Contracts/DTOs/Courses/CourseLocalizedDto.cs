using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{
  
    public record CourseLocalizedDto(
        Guid Id,
        Guid InstructorId,
        string InstructorName,
        Guid CategoryId,
        string CategorySlug,
        string? ThumbnailUrl,
        decimal Price,
        int DurationMinutes,
        string Level,
        string Status,
        string ResolvedLanguage,
        string Title,
        string ShortDescription,
        string Description,
        IReadOnlyList<string> WhatYouWillLearn,
        DateTime CreatedAt,
        DateTime? UpdatedAt,

        // --- Агрегаты для шапки страницы курса ---
        string CategoryName,
        double AverageRating,
        int ReviewCount,
        int EnrollmentCount,
        int LessonCount,

        // --- Программа курса: структура без содержимого уроков ---
        IReadOnlyList<CourseCurriculumModuleDto> Curriculum,

        // IsEnrolled — записан ли текущий пользователь. Именно это поле переключает
        // кнопку «Записаться» на «Продолжить обучение». Считает сервер, а не фронтенд:
        // у гостя всегда false, подделать подстановкой в localStorage нельзя.
        bool IsEnrolled);

}
