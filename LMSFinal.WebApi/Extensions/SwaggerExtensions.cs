using LMSFinal.WebApi.Swagger;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace LMSFinal.WebApi.Extensions
{
    public static class SwaggerExtensions
    {
        private const string SecuritySchemeName = "Bearer";

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LMSFinal API",
                    Version = "v1",
                    Description = BuildApiDescription(),
                    Contact = new OpenApiContact
                    {
                        Name = "Leyla Agabalayeva — Code Academy Final Project",
                        Email = "agabalayev.elchin02@gmail.com"
                    },
                    License = new OpenApiLicense { Name = "Educational use only" }
                });

                options.AddSecurityDefinition(SecuritySchemeName, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "JWT-авторизация.\n\n" +
                        "1. Выполните **POST /api/auth/login** и скопируйте значение `data.token`.\n" +
                        "2. Нажмите **Authorize** и вставьте ТОЛЬКО сам токен — без слова `Bearer` " +
                        "(Swagger добавит префикс сам).\n" +
                        "3. Нажмите Authorize → Close. Теперь защищённые эндпоинты будут " +
                        "отправлять заголовок `Authorization: Bearer <token>`."
                });

                options.OperationFilter<AuthorizeCheckOperationFilter>();
                options.OperationFilter<DefaultResponsesOperationFilter>();

                // --- XML-документация из /// комментариев ---
                IncludeXmlComments(options, Assembly.GetExecutingAssembly());             
                IncludeXmlComments(options, typeof(Contracts.Common.ApiResponse).Assembly); 

          
                options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
            });

            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app)
        {
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((document, _) => SwaggerTags.Apply(document));
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "LMSFinal API v1");
                options.DocumentTitle = "LMSFinal API — Documentation";

                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.DefaultModelsExpandDepth(0);
                options.EnableFilter();       
                options.DisplayRequestDuration(); 
            });

            return app;
        }

        private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        }

        private static string BuildApiDescription() =>
            """
            REST API системы онлайн-обучения (Online Learning Management System).

            ### Архитектура
            `WebApi (контроллеры) → Application (сервисы) → Persistence (репозитории + EF Core) → SQL Server`
            Бизнес-логики в контроллерах нет, наружу отдаются только DTO из проекта `LMSFinal.Contracts`.

            ### Единый формат ответа
            Все успешные ответы обёрнуты в `ApiResponse<T>`:
            ```json
            { "success": true, "message": "...", "data": { } }
            ```
            Все ошибки — в `ApiResponse` без `data`:
            ```json
            { "success": false, "message": "Course not found", "errors": null }
            ```
            Ошибки валидации (422) дополнительно содержат `errors` по полям:
            ```json
            { "success": false, "message": "Validation failed",
              "errors": { "title": ["Title is required"] } }
            ```
            Формирует их `GlobalExceptionMiddleware` — try/catch в контроллерах нет.

            ### Роли
            | Роль | Что может |
            |---|---|
            | `Student` | записываться на курсы, проходить уроки и тесты, оставлять отзывы, получать сертификаты |
            | `Instructor` | создавать курсы, модули, уроки, тесты; видеть своих студентов и аналитику |
            | `Admin` | управлять категориями (общая таксономия платформы) |

            Проверка владения (ownership) выполняется в сервисах: инструктор не может редактировать
            чужой курс, студент не видит чужой Grade Book. Такие попытки возвращают **403 Forbidden**.

            ### Как авторизоваться
            `POST /api/auth/login` → скопировать `data.token` → кнопка **Authorize** вверху справа.
            """;
    }
}
