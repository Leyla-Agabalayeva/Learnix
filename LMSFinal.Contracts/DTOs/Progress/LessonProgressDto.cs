using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Progress
{

    public record LessonProgressDto(
        Guid Id,
        Guid LessonId,
        bool IsCompleted,
        DateTime? CompletedAt,
        int LastPosition,
        DateTime LastAccessedAt);
}
