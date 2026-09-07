using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class QuizAttemptAnswerSelection : BaseEntity
    {
        public Guid QuizAttemptAnswerId { get; set; }
        public QuizAttemptAnswer QuizAttemptAnswer { get; set; } = null!;

        public Guid AnswerId { get; set; }
        public Answer Answer { get; set; } = null!;
    }
}
