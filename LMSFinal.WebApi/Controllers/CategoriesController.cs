using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Categories;
using LMSFinal.Domain.Enums;
using LMSFinal.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

  
        /// <param name="lang">Необязательный язык ответа: Az, En или Ru.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список категорий.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] LanguageCode? lang, CancellationToken cancellationToken)
        {
            if (lang.HasValue)
            {
                var localized = await _categoryService.GetAllLocalizedAsync(lang.Value, cancellationToken);
                return Success(localized);
            }

            var categories = await _categoryService.GetAllAsync(cancellationToken);
            return Success(categories);
        }

    
        /// <param name="request">Slug, иконка и переводы названия.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="201">Категория создана.</response>
        /// <response code="409">Категория с таким Slug уже существует.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        {
            var category = await _categoryService.CreateAsync(request, cancellationToken);
            return Created(category);
        }

        /// <param name="id">Идентификатор категории.</param>
        /// <param name="request">Slug, иконка и переводы названия.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Категория обновлена.</response>
        /// <response code="404">Категория не найдена.</response>
        /// <response code="409">Slug уже занят другой категорией.</response>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
        {
            var category = await _categoryService.UpdateAsync(id, request, cancellationToken);
            return Success(category);
        }

        /// <param name="id">Идентификатор категории.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="204">Категория удалена.</response>
        /// <response code="404">Категория не найдена.</response>
        /// <response code="409">В категории есть курсы — сначала перенесите их.</response>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _categoryService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
