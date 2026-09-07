using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{


    public interface IModuleRepository : IGenericRepository<Module>
    {
        Task<IReadOnlyList<Module>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<Module?> GetWithCourseAsync(Guid moduleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Module>> GetByIdsWithCourseAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        Task<int> GetNextOrderIndexAsync(Guid courseId, CancellationToken cancellationToken = default);
    }

}
