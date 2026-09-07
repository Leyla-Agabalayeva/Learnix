using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Progress
{

    public record ContinueLearningDto(
        Guid? LessonId,
        Guid? ModuleId,
        string? LessonTitle,
        bool CourseCompleted);
}
