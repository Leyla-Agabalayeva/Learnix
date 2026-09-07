using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Certificates
{
    public record CertificateDto(
        Guid Id,
        string CertificateNumber,
        Guid StudentId,
        string StudentName,
        Guid CourseId,
        string CourseTitle,
        string InstructorName,
        DateTime IssuedAt,
        DateTime CompletionDate);

}
