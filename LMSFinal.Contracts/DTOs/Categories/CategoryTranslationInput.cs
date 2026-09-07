using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Categories
{
    public record CategoryTranslationInput(LanguageCode LanguageCode, string Name, string? Description);

}
