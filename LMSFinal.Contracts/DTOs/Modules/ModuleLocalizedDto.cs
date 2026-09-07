using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Modules
{
    public record ModuleLocalizedDto(
        Guid Id,
        Guid CourseId,
        int OrderIndex,
        string ResolvedLanguage,
        string Title,
        string? Description);
}
