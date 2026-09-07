using LMSFinal.Domain.Enums;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuizTranslationInput(LanguageCode LanguageCode, string Title, string? Description);
}
