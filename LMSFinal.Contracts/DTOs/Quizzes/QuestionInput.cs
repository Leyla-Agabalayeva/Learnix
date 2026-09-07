using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{
    public record QuestionInput(
      IReadOnlyList<QuestionTranslationInput> Translations,
      QuestionType QuestionType,
      int Points,
      IReadOnlyList<AnswerInput> Answers);

}
