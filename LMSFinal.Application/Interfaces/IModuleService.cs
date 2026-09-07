using LMSFinal.Contracts.DTOs.Modules;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface IModuleService
    {
        Task<IReadOnlyList<ModuleDto>> GetByCourseIdAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ModuleLocalizedDto>> GetByCourseIdLocalizedAsync(Guid currentUserId, Guid courseId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<ModuleDto> CreateAsync(Guid instructorId, Guid courseId, CreateModuleRequest request, CancellationToken cancellationToken = default);

        Task<ModuleDto> UpdateAsync(Guid instructorId, Guid moduleId, UpdateModuleRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid instructorId, Guid moduleId, CancellationToken cancellationToken = default);

        Task ReorderAsync(Guid instructorId, ReorderModulesRequest request, CancellationToken cancellationToken = default);
    }




}
