using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{

    public record QuizResultDto(
        Guid AttemptId,
        string? StudentName,
        int Score,
        double Percentage,
        bool Passed,
        int? TotalQuestions,
        int? CorrectAnswers,
        DateTime? CompletedAt);

}
