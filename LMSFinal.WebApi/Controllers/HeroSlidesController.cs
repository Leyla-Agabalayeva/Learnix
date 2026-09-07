using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Home;
using LMSFinal.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/hero-slides")]
    [Produces("application/json")]
    public class HeroSlidesController : ApiControllerBase
    {
        private readonly IHeroSlideService _heroSlideService;

        public HeroSlidesController(IHeroSlideService heroSlideService)
        {
            _heroSlideService = heroSlideService;
        }

        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Слайды карусели на главной, по порядку.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HeroSlideDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var slides = await _heroSlideService.GetAllAsync(cancellationToken);
            return Success(slides);
        }
    }
}
