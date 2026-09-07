using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Wishlist;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/wishlist")]
    [Authorize(Roles = "Student")]
    [Produces("application/json")]
    public class WishlistController : ApiControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список избранных курсов с краткой карточкой каждого.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WishlistItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy([FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _wishlistService.GetMyWishlistLocalizedAsync(
                    User.GetUserId(), lang.Value, cancellationToken);
                return Success(localized);
            }

            var items = await _wishlistService.GetMyWishlistAsync(User.GetUserId(), cancellationToken);
            return Success(items);
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Курс в избранном.</response>
        /// <response code="404">Курс не найден.</response>
        [HttpPost("{courseId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Add(Guid courseId, CancellationToken cancellationToken)
        {
            await _wishlistService.AddAsync(User.GetUserId(), courseId, cancellationToken);
            return NoContent();
        }
        /// <param name="courseId">Идентификатор курса.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Курса нет в избранном.</response>
        [HttpDelete("{courseId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Remove(Guid courseId, CancellationToken cancellationToken)
        {
            await _wishlistService.RemoveAsync(User.GetUserId(), courseId, cancellationToken);
            return NoContent();
        }
    }
}
