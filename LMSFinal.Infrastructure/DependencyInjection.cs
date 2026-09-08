using LMSFinal.Application.Interfaces;
using LMSFinal.Infrastructure.AI;
using LMSFinal.Infrastructure.Email;
using LMSFinal.Infrastructure.Identity;
using LMSFinal.Infrastructure.Pdf;
using LMSFinal.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Minio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            services.Configure<JwtSettings>(jwtSection);

            var jwtSettings = jwtSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException("Секция 'Jwt' не найдена в конфигурации (appsettings.Development.json).");

            if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key отсутствует или короче 32 символов. Сгенерируйте длинный случайный ключ (см. README_PHASE3.md).");
            }

            services.Configure<EmailSettings>(configuration.GetSection("Email"));
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<RoleSeeder>();
            services.AddScoped<AdminSeeder>();
            services.AddScoped<ICertificatePdfGenerator, QuestPdfCertificateGenerator>();

            var storageSection = configuration.GetSection("Storage");
            services.Configure<StorageSettings>(storageSection);
            var storageSettings = storageSection.Get<StorageSettings>()
                ?? throw new InvalidOperationException("Секция 'Storage' не найдена в конфигурации.");

            services.AddSingleton<IMinioClient>(_ => new MinioClient()
                .WithEndpoint(storageSettings.Endpoint)
                .WithCredentials(storageSettings.AccessKey, storageSettings.SecretKey)
                .WithSSL(storageSettings.UseSsl)
                .Build());

            services.AddScoped<IFileStorageService, MinioFileStorageService>();

            services.Configure<DeepSeekSettings>(configuration.GetSection("DeepSeek"));
            services.AddHttpClient<IQuizGenerationService, DeepSeekQuizGenerationService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Браузер не может выставить заголовок Authorization на WebSocket-соединении,
                // поэтому SignalR передаёт токен через query string — берём его оттуда только для /hubs.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }

}
