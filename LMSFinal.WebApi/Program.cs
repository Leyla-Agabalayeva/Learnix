using LMSFinal.Application.Interfaces;
using LMSFinal.Application.Services;
using LMSFinal.Domain.Entities;
using LMSFinal.Infrastructure;
using LMSFinal.Infrastructure.Identity;
using LMSFinal.Persistence;
using LMSFinal.Persistence.Data;
using LMSFinal.Persistence.Seed;
using LMSFinal.WebApi.Extensions;
using LMSFinal.WebApi.Filters;
using LMSFinal.WebApi.Hubs;
using LMSFinal.WebApi.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence  ---
builder.Services.AddPersistence(builder.Configuration);

// --- Identity  ---
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- JWT + Auth services  ---
builder.Services.AddInfrastructure(builder.Configuration);

// --- Application: AutoMapper + FluentValidation + Services  ---
builder.Services.AddApplication();

// --- Controllers + глобальный ValidationFilter  ---
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// --- SignalR: живая доставка уведомлений поверх уже существующего NotificationService ---
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();

// --- Swagger / OpenAPI  ---
// Вся настройка — в Extensions/SwaggerExtensions.cs
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// --- Фронтенд из wwwroot ---
app.UseDefaultFiles();

var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".sql"] = "application/sql";
contentTypes.Mappings[".md"] = "text/markdown; charset=utf-8";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "no-cache, must-revalidate";
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

await SeedDatabaseAsync(app);

app.Run();
static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      
        await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        await services.GetRequiredService<RoleSeeder>().SeedAsync();
        await services.GetRequiredService<AdminSeeder>().SeedAsync();
        await services.GetRequiredService<DemoDataSeeder>().SeedAsync();

        await LMSFinal.Infrastructure.Storage.MinioBucketInitializer.EnsureBucketsAsync(
            services.GetRequiredService<Minio.IMinioClient>());
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Сидирование базы завершилось ошибкой. Приложение продолжит работу без демо-данных.");
    }
}
