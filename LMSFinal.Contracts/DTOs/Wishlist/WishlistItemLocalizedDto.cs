using LMSFinal.Contracts.DTOs.Courses;

namespace LMSFinal.Contracts.DTOs.Wishlist
{
    public record WishlistItemLocalizedDto(
        Guid Id,
        CourseSummaryLocalizedDto Course,
        DateTime AddedAt);
}
