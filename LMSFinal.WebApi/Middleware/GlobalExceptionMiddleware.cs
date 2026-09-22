using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LMSFinal.WebApi.Middleware
{

    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, response) = exception switch
            {
                ValidationAppException ex => (
                    StatusCodes.Status422UnprocessableEntity,
                    ApiResponse.Fail("Validation failed", ex.Errors)),

                NotFoundException ex => (
                    StatusCodes.Status404NotFound,
                    ApiResponse.Fail(ex.Message)),

                ForbiddenAccessException ex => (
                    StatusCodes.Status403Forbidden,
                    ApiResponse.Fail(ex.Message)),

                ConflictException ex => (
                    StatusCodes.Status409Conflict,
                    ApiResponse.Fail(ex.Message)),

                DbUpdateConcurrencyException => (
       StatusCodes.Status409Conflict,
       ApiResponse.Fail("Данные были изменены другим запросом. Обновите страницу и повторите.")),

                // Отдельно от JsonException ниже: это НЕ "клиент прислал кривой JSON",
                // а "внешний AI-сервис ответил, но результат нельзя разобрать" — DeepSeek
                // изредка обрезает ответ по лимиту токенов. 502, а не 400: проблема на
                // стороне апстрима, форма клиента тут ни при чём.
                AiGenerationException ex => (
                    StatusCodes.Status502BadGateway,
                    ApiResponse.Fail(ex.Message)),

                System.Text.Json.JsonException => (
                    StatusCodes.Status400BadRequest,
                    ApiResponse.Fail("Проверьте правильность заполнения полей формы.")),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("Внутренняя ошибка сервера."))
            };
            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
            }
            else if (exception is AiGenerationException)
            {
                // Не "unhandled" — но по-тихому такое лучше не проглатывать: без этого
                // прошлый раз пришлось лезть в код, чтобы понять, что вообще случилось.
                _logger.LogWarning(exception, "AI generation failed on {Path}", context.Request.Path);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

}
