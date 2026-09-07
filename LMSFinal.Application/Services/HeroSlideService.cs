using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Home;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Interfaces;

namespace LMSFinal.Application.Services
{
    public class HeroSlideService : IHeroSlideService
    {
        private readonly IHeroSlideRepository _heroSlideRepository;
        private readonly IUnitOfWork _unitOfWork;

        public HeroSlideService(IHeroSlideRepository heroSlideRepository, IUnitOfWork unitOfWork)
        {
            _heroSlideRepository = heroSlideRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<HeroSlideDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var slides = await _heroSlideRepository.GetAllOrderedAsync(cancellationToken);
            return slides.Select(Map).ToList();
        }

        public async Task<HeroSlideDto> CreateAsync(string imageUrl, string? linkUrl, CancellationToken cancellationToken = default)
        {
            var slide = new HeroSlide
            {
                ImageUrl = imageUrl,
                LinkUrl = linkUrl,
                OrderIndex = await _heroSlideRepository.GetNextOrderIndexAsync(cancellationToken)
            };

            await _heroSlideRepository.AddAsync(slide, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(slide);
        }

        public async Task<string> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var slide = await _heroSlideRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("HeroSlide", id);

            _heroSlideRepository.Remove(slide);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return slide.ImageUrl;
        }

        private static HeroSlideDto Map(HeroSlide slide) =>
            new(slide.Id, slide.ImageUrl, slide.LinkUrl, slide.OrderIndex);
    }
}
