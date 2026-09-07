using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class Question : BaseEntity
    {
        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;

        public int OrderIndex { get; set; }

        public int Points { get; set; } = 1;

        // Навигационные свойства
        public ICollection<QuestionTranslation> Translations { get; set; } = new List<QuestionTranslation>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }

}
