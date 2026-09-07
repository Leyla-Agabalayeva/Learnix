using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class QuizAttemptAnswer : BaseEntity
    {
        public Guid QuizAttemptId { get; set; }
        public QuizAttempt QuizAttempt { get; set; } = null!;

        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        // Навигационные свойства
        // Для Single Choice — одна запись, для Multiple Choice — несколько.
        public ICollection<QuizAttemptAnswerSelection> SelectedAnswers { get; set; } = new List<QuizAttemptAnswerSelection>();
    }

}
