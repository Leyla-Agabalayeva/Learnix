using LMSFinal.Contracts.DTOs.Enrollments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Domain.Enums;

namespace LMSFinal.Application.Interfaces
{
    public interface IEnrollmentService
    {
  
        Task<StudentEnrollmentDto> EnrollAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentEnrollmentDto>> GetMyEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);


        Task<IReadOnlyList<StudentEnrollmentLocalizedDto>> GetMyEnrollmentsLocalizedAsync(
            Guid studentId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CourseEnrollmentDto>> GetCourseStudentsAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default);


        Task<StudentEnrollmentDto> CancelAsync(Guid studentId, Guid enrollmentId, CancellationToken cancellationToken = default);
    }

}
