using LMSFinal.Contracts.DTOs.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{

    public interface ICertificateService
    {
      
        Task<CertificateDto?> IssueIfEligibleAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CertificateDto>> GetMyCertificatesAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<CertificateDto> GetByIdAsync(Guid studentId, Guid certificateId, CancellationToken cancellationToken = default);

        Task<CertificateVerificationDto> VerifyAsync(string certificateNumber, CancellationToken cancellationToken = default);

        Task<byte[]> GeneratePdfAsync(Guid studentId, Guid certificateId, CancellationToken cancellationToken = default);
    }

}
