using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Categories
{
    public record CategoryLocalizedDto(
       Guid Id,
       string Slug,
       string? IconUrl,
       string ResolvedLanguage,
       string Name,
       string? Description);
}
