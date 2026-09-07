using LMSFinal.Contracts.DTOs.Courses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Enrollments
{

    /// <summary>Для "Мои курсы" студента — что за курс и на каком он этапе.</summary>
    public record StudentEnrollmentDto(
        Guid Id,
        DateTime EnrolledAt,
        DateTime? CompletedAt,
        double ProgressPercentage,
        string Status,
        CourseSummaryDto Course);

}
