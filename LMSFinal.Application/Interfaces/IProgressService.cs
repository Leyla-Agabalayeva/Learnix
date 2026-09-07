using LMSFinal.Contracts.DTOs.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface IProgressService
    {
        Task<LessonProgressDto> MarkCompleteAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken = default);

        Task<CourseProgressDto> GetCourseProgressAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task<ContinueLearningDto> GetContinueLearningAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    }

}
