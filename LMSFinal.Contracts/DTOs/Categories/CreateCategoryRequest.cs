using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Categories
{

    public record CreateCategoryRequest
    {
        public string Slug { get; init; } = string.Empty;

        public string? IconUrl { get; init; }

        public IReadOnlyList<CategoryTranslationInput> Translations { get; init; } = Array.Empty<CategoryTranslationInput>();
    }

}
