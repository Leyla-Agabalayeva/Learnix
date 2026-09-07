using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Certificates
{

    public record CertificateVerificationDto(
        string CertificateNumber,
        string StudentName,
        string CourseTitle,
        string InstructorName,
        DateTime IssuedAt,
        DateTime CompletionDate);
}
