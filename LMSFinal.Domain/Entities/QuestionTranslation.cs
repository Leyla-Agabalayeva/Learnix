using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Domain.Entities
{
    public class QuestionTranslation : BaseEntity
    {
        public Guid QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string QuestionText { get; set; } = string.Empty;
    }
}
