using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.GradeBook
{
    public record GradeBookDto(GradeBookSummaryDto Summary, IReadOnlyList<GradeBookRowDto> Rows);
}
