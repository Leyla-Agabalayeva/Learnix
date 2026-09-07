using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Domain.Entities
{
    public class QuizTranslation : BaseEntity
    {
        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
