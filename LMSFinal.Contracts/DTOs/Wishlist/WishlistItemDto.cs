using LMSFinal.Contracts.DTOs.Courses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Wishlist
{
    public record WishlistItemDto(Guid Id, CourseSummaryDto Course, DateTime AddedAt);

}
