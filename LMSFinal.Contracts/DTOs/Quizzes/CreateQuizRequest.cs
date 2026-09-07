using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Quizzes
{



    public record CreateQuizRequest
    {
        public Guid LessonId { get; init; }

        public IReadOnlyList<QuizTranslationInput> Translations { get; init; } = Array.Empty<QuizTranslationInput>();

        public int PassingScore { get; init; } = 60;

        public int? TimeLimitMinutes { get; init; }

        public IReadOnlyList<QuestionInput> Questions { get; init; } = Array.Empty<QuestionInput>();
    }

}
