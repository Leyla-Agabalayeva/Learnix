using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Reviews
{
    public record UpdateReviewRequest
    {
        public int Rating { get; init; }

        public string? Comment { get; init; }
    }

}
