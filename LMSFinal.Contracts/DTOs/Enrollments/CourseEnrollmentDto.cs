using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Enrollments
{
    /// <summary>Для инструктора — список студентов, записанных на его курс.</summary>
    public record CourseEnrollmentDto(
        Guid Id,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        DateTime EnrolledAt,
        DateTime? CompletedAt,
        double ProgressPercentage,
        string Status);

}
