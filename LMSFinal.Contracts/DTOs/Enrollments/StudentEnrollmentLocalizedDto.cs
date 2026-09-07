using LMSFinal.Contracts.DTOs.Courses;

namespace LMSFinal.Contracts.DTOs.Enrollments
{

    
    /// Отличается от StudentEnrollmentDto тем, что внутри лежит
    /// CourseSummaryLocalizedDto — та же карточка, что в каталоге, с рейтингом
    /// и преподавателем. Благодаря этому «Мои курсы» выглядят так же, как каталог,
    /// а не как его урезанная версия.
    
    public record StudentEnrollmentLocalizedDto(
        Guid Id,
        DateTime EnrolledAt,
        DateTime? CompletedAt,
        double ProgressPercentage,
        string Status,
        CourseSummaryLocalizedDto Course);
}
