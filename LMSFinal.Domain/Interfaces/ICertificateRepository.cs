using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{

    public interface ICertificateRepository : IGenericRepository<Certificate>
    {
        Task<IReadOnlyList<Certificate>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default);
        Task<Certificate?> GetWithDetailsAsync(Guid certificateId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
        Task<int> CountByYearAsync(int year, CancellationToken cancellationToken = default);
    }

}
