using System;

namespace LMSFinal.Contracts.DTOs.Quizzes
{

  
    public record AnswerLocalizedDto(Guid Id, string AnswerText, bool? IsCorrect);
}
