# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore as a distinct layer for better caching: copy only project files first.
COPY Directory.Build.props ./
COPY src/ReservationSystem.Api/ReservationSystem.Api.csproj src/ReservationSystem.Api/
COPY src/ReservationSystem.Application/ReservationSystem.Application.csproj src/ReservationSystem.Application/
COPY src/ReservationSystem.Domain/ReservationSystem.Domain.csproj src/ReservationSystem.Domain/
COPY src/ReservationSystem.Infrastructure/ReservationSystem.Infrastructure.csproj src/ReservationSystem.Infrastructure/
RUN dotnet restore src/ReservationSystem.Api/ReservationSystem.Api.csproj

# Copy the remaining source and publish a framework-dependent build.
COPY src/ ./src/
RUN dotnet publish src/ReservationSystem.Api/ReservationSystem.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as the image's built-in non-root user.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReservationSystem.Api.dll"]
