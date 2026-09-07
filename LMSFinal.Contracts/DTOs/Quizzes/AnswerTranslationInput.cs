using LMSFinal.Domain.Enums;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record AnswerTranslationInput(LanguageCode LanguageCode, string AnswerText);
}
