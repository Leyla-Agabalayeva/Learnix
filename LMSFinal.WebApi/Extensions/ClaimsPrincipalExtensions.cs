using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSFinal.WebApi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("Токен не содержит корректный идентификатор пользователя.");
            }

            return userId;
        }
    }

}
