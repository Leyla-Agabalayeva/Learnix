using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Domain.Entities
{
    public class AnswerTranslation : BaseEntity
    {
        public Guid AnswerId { get; set; }
        public Answer Answer { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string AnswerText { get; set; } = string.Empty;
    }
}
