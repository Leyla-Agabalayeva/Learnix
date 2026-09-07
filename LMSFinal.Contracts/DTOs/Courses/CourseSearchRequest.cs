using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Courses
{
    public class CourseSearchRequest
    {
        public string? SearchTerm { get; set; }

        public Guid? CategoryId { get; set; }

        public CourseLevel? Level { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }


        public CourseSortBy SortBy { get; set; } = CourseSortBy.Newest;

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }

}
