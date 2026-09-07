using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{

    public class Answer : BaseEntity
    {
        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public bool IsCorrect { get; set; } = false;

        public ICollection<AnswerTranslation> Translations { get; set; } = new List<AnswerTranslation>();
    }
}
