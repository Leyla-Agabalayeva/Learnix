using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.GradeBook
{

    public record GradeBookRowDto(
        Guid AttemptId,
        Guid CourseId,
        string CourseTitle,
        Guid QuizId,
        string QuizTitle,
        int Score,
        double Percentage,
        bool Passed,
        DateTime? CompletedAt);

}
