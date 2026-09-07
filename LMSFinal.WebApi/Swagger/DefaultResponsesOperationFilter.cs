using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMSFinal.WebApi.Swagger
{
    public class DefaultResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasRequestBody = context.ApiDescription.ParameterDescriptions
                .Any(parameter => parameter.Source == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body);

            if (hasRequestBody)
            {
                operation.Responses.TryAdd("422", new OpenApiResponse
                {
                    Description = "Ошибка валидации. Тело ответа содержит errors — словарь " +
                                  "\"поле → список сообщений\" (FluentValidation)."
                });
            }

            operation.Responses.TryAdd("500", new OpenApiResponse
            {
                Description = "Внутренняя ошибка сервера. Ответ в едином формате ApiResponse, " +
                              "детали исключения наружу не отдаются и пишутся только в лог."
            });
        }
    }
}
