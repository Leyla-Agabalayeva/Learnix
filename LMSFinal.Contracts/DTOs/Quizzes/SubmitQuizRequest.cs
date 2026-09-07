using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record SubmitQuizRequest
    {
        public IReadOnlyList<SubmitQuizAnswerInput> Answers { get; init; } = Array.Empty<SubmitQuizAnswerInput>();
    }
}
