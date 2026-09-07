using LMSFinal.Domain.Enums;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuestionTranslationInput(LanguageCode LanguageCode, string QuestionText);
}
