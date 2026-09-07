using LMSFinal.Contracts.DTOs.Home;

namespace LMSFinal.Application.Interfaces
{
    public interface IHeroSlideService
    {
        Task<IReadOnlyList<HeroSlideDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<HeroSlideDto> CreateAsync(string imageUrl, string? linkUrl, CancellationToken cancellationToken = default);

        /// <returns>ImageUrl удалённого слайда — вызывающий код (контроллер) сам удаляет файл из хранилища.</returns>
        Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
