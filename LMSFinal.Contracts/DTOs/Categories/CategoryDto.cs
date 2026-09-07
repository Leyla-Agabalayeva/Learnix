using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Categories
{

    public record CategoryDto(
        Guid Id,
        string Slug,
        string? IconUrl,
        IReadOnlyList<CategoryTranslationDto> Translations);
}
