using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Categories
{
    public record CategoryTranslationDto(string LanguageCode, string Name, string? Description);

}
