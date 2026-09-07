using LMSFinal.Domain.Common;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Entities
{
    public class ModuleTranslation : BaseEntity
    {
        public Guid ModuleId { get; set; }
        public Module Module { get; set; } = null!;

        public LanguageCode LanguageCode { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

}
