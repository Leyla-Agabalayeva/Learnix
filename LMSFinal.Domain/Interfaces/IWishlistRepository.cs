using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Interfaces
{
    public interface IWishlistRepository : IGenericRepository<Wishlist>
    {
        Task<IReadOnlyList<Wishlist>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Wishlist>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

        Task RemoveAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    }


}
