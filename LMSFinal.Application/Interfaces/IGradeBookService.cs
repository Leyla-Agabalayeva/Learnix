using LMSFinal.Contracts.DTOs.GradeBook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{


    public interface IGradeBookService
    {
        Task<GradeBookDto> GetMyGradeBookAsync(Guid studentId, CancellationToken cancellationToken = default);
    }
}
