# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore — copy csproj files first for layer caching
COPY src/MyMarina.Domain/MyMarina.Domain.csproj             src/MyMarina.Domain/
COPY src/MyMarina.Application/MyMarina.Application.csproj   src/MyMarina.Application/
COPY src/MyMarina.Infrastructure/MyMarina.Infrastructure.csproj src/MyMarina.Infrastructure/
COPY src/MyMarina.Api/MyMarina.Api.csproj                   src/MyMarina.Api/
RUN dotnet restore src/MyMarina.Api/MyMarina.Api.csproj

# Build
COPY src/ src/
RUN dotnet publish src/MyMarina.Api/MyMarina.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Non-root user
RUN groupadd --system --gid 1001 appgroup \
 && useradd  --system --uid 1001 --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MyMarina.Api.dll"]
