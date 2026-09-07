using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
    public interface ILessonRepository : IGenericRepository<Lesson>
    {
        Task<IReadOnlyList<Lesson>> GetByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Lesson>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<Lesson?> GetWithModuleAndCourseAsync(Guid lessonId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Lesson>> GetByIdsWithModuleAndCourseAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

        Task<int> GetNextOrderIndexAsync(Guid moduleId, CancellationToken cancellationToken = default);
    }


}
