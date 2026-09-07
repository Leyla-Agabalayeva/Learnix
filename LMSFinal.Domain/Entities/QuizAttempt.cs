using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class QuizAttempt : BaseEntity
    {
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public int Score { get; set; }

        public double Percentage { get; set; }

        public bool Passed { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // Навигационные свойства
        public ICollection<QuizAttemptAnswer> AttemptAnswers { get; set; } = new List<QuizAttemptAnswer>();
    }

}
