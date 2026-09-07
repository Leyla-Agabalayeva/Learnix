using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Modules
{
    public record ModuleDto(
        Guid Id,
        Guid CourseId,
        int OrderIndex,
        IReadOnlyList<ModuleTranslationDto> Translations);

}
