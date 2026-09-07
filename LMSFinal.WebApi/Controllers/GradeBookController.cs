using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.GradeBook;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
    [Route("api/gradebook")]
    [Authorize(Roles = "Student")]
    [Produces("application/json")]
    public class GradeBookController : ApiControllerBase
    {
        private readonly IGradeBookService _gradeBookService;

        public GradeBookController(IGradeBookService gradeBookService)
        {
            _gradeBookService = gradeBookService;
        }
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Ведомость текущего студента.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<GradeBookDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
        {
            var gradeBook = await _gradeBookService.GetMyGradeBookAsync(User.GetUserId(), cancellationToken);
            return Success(gradeBook);
        }
    }
}
