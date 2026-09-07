using LMSFinal.Contracts.DTOs.Wishlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LMSFinal.Domain.Enums;

namespace LMSFinal.Application.Interfaces
{

    public interface IWishlistService
    {
        Task<IReadOnlyList<WishlistItemDto>> GetMyWishlistAsync(Guid studentId, CancellationToken cancellationToken = default);

      
        Task<IReadOnlyList<WishlistItemLocalizedDto>> GetMyWishlistLocalizedAsync(
            Guid studentId, LanguageCode language, CancellationToken cancellationToken = default);

   
        Task AddAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

    
        Task RemoveAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    }

}
