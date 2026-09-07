using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.GradeBook
{

    public record GradeBookSummaryDto(
        double AverageQuizScore,
        int CompletedQuizzes,
        int PassedQuizzes,
        double AverageCourseCompletion,
        int EnrolledCoursesCount,
        int CompletedCoursesCount);

}
