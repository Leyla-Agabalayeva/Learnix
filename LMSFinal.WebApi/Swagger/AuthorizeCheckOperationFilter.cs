using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMSFinal.WebApi.Swagger
{
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            var authorizeData = metadata.OfType<IAuthorizeData>().ToList();

            if (authorizeData.Count == 0)
            {
                return; 
            }

            operation.Responses.TryAdd("401", new OpenApiResponse
            {
                Description = "Не аутентифицирован: JWT отсутствует, просрочен или некорректен."
            });

            var roles = authorizeData
                .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (roles.Count > 0)
            {
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = $"Доступ запрещён: требуется роль {string.Join(" или ", roles)}, " +
                                  "либо ресурс принадлежит другому пользователю (ownership check)."
                });

                operation.Description =
                    $"**Требуется роль:** `{string.Join("` / `", roles)}`\n\n{operation.Description}";
            }
            else
            {
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Доступ запрещён: ресурс принадлежит другому пользователю (ownership check)."
                });

                operation.Description = $"**Требуется авторизация** (любая роль)\n\n{operation.Description}";
            }

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            };
        }
    }
}
