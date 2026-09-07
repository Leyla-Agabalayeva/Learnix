using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace LMSFinal.Persistence.Repositories
{
    public class HeroSlideRepository : BaseRepository<HeroSlide>, IHeroSlideRepository
    {
        public HeroSlideRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<HeroSlide>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
            await Context.HeroSlides
                .OrderBy(s => s.OrderIndex)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        public async Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken = default)
        {
            var maxOrder = await Context.HeroSlides
                .Select(s => (int?)s.OrderIndex)
                .MaxAsync(cancellationToken);

            return (maxOrder ?? -1) + 1;
        }
    }
}
