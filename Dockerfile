# --- Этап сборки -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов ОТДЕЛЬНО от кода — так Docker кэширует restore
# и не переустанавливает пакеты при каждой правке .cs файла.
COPY LMSFinal.sln .
COPY LMSFinal.Domain/*.csproj LMSFinal.Domain/
COPY LMSFinal.Contracts/*.csproj LMSFinal.Contracts/
COPY LMSFinal.Application/*.csproj LMSFinal.Application/
COPY LMSFinal.Persistence/*.csproj LMSFinal.Persistence/
COPY LMSFinal.Infrastructure/*.csproj LMSFinal.Infrastructure/
COPY LMSFinal.WebApi/*.csproj LMSFinal.WebApi/
COPY LMSFinal.Tests/*.csproj LMSFinal.Tests/

RUN dotnet restore LMSFinal.sln

COPY . .
RUN dotnet publish LMSFinal.WebApi/LMSFinal.WebApi.csproj -c Release -o /app/publish

# --- Этап запуска ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LMSFinal.WebApi.dll"]
