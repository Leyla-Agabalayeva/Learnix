using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Certificates;
using LMSFinal.WebApi.Common;
using LMSFinal.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSFinal.WebApi.Controllers
{
  
    [Route("api/certificates")]
    [Produces("application/json")]
    public class CertificatesController : ApiControllerBase
    {
        private readonly ICertificateService _certificateService;

        public CertificatesController(ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Список сертификатов текущего студента.</response>
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CertificateDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
        {
            var certificates = await _certificateService.GetMyCertificatesAsync(User.GetUserId(), cancellationToken);
            return Success(certificates);
        }
        /// <param name="id">Идентификатор сертификата.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Данные сертификата.</response>
        /// <response code="403">Сертификат принадлежит другому студенту.</response>
        /// <response code="404">Сертификат не найден.</response>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<CertificateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var certificate = await _certificateService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
            return Success(certificate);
        }

        /// <param name="certificateNumber">Номер сертификата, например LMS-2026-000001.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">Сертификат действителен, возвращены имя студента, курс и дата.</response>
        /// <response code="404">Сертификат с таким номером не найден.</response>
        [HttpGet("verify/{certificateNumber}")]
        [ProducesResponseType(typeof(ApiResponse<CertificateVerificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Verify(string certificateNumber, CancellationToken cancellationToken)
        {
            var certificate = await _certificateService.VerifyAsync(certificateNumber, cancellationToken);
            return Success(certificate);
        }

        /// <param name="id">Идентификатор сертификата.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <response code="200">PDF-файл сертификата.</response>
        /// <response code="403">Сертификат принадлежит другому студенту.</response>
        /// <response code="404">Сертификат не найден.</response>
        [HttpGet("{id:guid}/download")]
        [Authorize(Roles = "Student")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            var pdfBytes = await _certificateService.GeneratePdfAsync(User.GetUserId(), id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"certificate-{id}.pdf");
        }
    }
}
