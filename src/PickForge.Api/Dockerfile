# PickForge API Dockerfile
# Multi-stage build for optimized production image

# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files first (better layer caching)
COPY ["PickForge.sln", "./"]
COPY ["src/PickForge.Api/PickForge.Api.csproj", "src/PickForge.Api/"]

# Restore dependencies (cached unless project files change)
RUN dotnet restore "src/PickForge.Api/PickForge.Api.csproj"

# Copy remaining source files
COPY ["src/PickForge.Api/", "src/PickForge.Api/"]

# Build the project
WORKDIR "/src/src/PickForge.Api"
RUN dotnet build "PickForge.Api.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "PickForge.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app

# Copy published output from publish stage
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PickForge.Api.dll"]
