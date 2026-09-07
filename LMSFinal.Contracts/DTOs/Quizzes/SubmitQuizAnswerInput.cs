using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{


    public record SubmitQuizAnswerInput(Guid QuestionId, IReadOnlyList<Guid> SelectedAnswerIds);
}
