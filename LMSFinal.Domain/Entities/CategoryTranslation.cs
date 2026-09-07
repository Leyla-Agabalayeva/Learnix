using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class CategoryTranslation : BaseEntity
    {
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

}
