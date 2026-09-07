using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{
    public record ReorderLessonsRequest
    {
        public IReadOnlyList<LessonOrderItem> Items { get; init; } = Array.Empty<LessonOrderItem>();
    }
}
