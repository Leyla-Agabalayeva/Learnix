using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
   
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        Task<Enrollment?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Enrollment>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
    }

}
