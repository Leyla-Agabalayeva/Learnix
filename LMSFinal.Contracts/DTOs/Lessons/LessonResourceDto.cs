using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Lessons
{
    public record LessonResourceDto(Guid Id, string FileName, string FileUrl, string FileType);
}
