using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Reviews
{
    public record CourseReviewDto(
      Guid Id,
      Guid CourseId,
      Guid StudentId,
      string StudentName,
      int Rating,
      string? Comment,
      DateTime CreatedAt,
      DateTime? UpdatedAt);
}
