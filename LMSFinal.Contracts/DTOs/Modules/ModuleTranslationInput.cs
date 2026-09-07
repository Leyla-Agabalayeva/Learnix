using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Modules
{
    public record ModuleTranslationInput(LanguageCode LanguageCode, string Title, string? Description);
}
