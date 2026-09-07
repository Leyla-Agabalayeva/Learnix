using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Modules
{

    public record CreateModuleRequest
    {
        public IReadOnlyList<ModuleTranslationInput> Translations { get; init; } = Array.Empty<ModuleTranslationInput>();
    }
}
