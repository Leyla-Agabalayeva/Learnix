using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Progress
{
    public record CourseProgressDto(
      Guid CourseId,
      int TotalLessons,
      int CompletedLessons,
      double ProgressPercentage,
      bool IsCourseCompleted,
      IReadOnlyList<Guid> CompletedLessonIds);
}
