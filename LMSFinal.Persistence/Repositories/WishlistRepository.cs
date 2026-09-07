using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Persistence.Repositories
{
    public class WishlistRepository : BaseRepository<Wishlist>, IWishlistRepository
    {
        public WishlistRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Wishlist>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            await Context.Wishlists
                .Include(w => w.Course).ThenInclude(c => c.Translations)
                .Include(w => w.Course).ThenInclude(c => c.Instructor)
                .Where(w => w.StudentId == studentId)
                .OrderByDescending(w => w.AddedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Wishlist>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Wishlists
                .Where(w => w.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
            await Context.Wishlists.AnyAsync(w => w.StudentId == studentId && w.CourseId == courseId, cancellationToken);

        public async Task RemoveAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var item = await Context.Wishlists
                .FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId, cancellationToken);

            if (item is not null)
            {
                Context.Wishlists.Remove(item);
            }
        }
    }
    }
