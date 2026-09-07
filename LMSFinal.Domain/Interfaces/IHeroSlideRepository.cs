using LMSFinal.Domain.Entities;

namespace LMSFinal.Domain.Interfaces
{
    public interface IHeroSlideRepository : IGenericRepository<HeroSlide>
    {
        Task<IReadOnlyList<HeroSlide>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

        Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken = default);
    }
}
