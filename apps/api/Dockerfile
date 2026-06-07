# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore
COPY VyaaparNexus.Domain/VyaaparNexus.Domain.csproj          VyaaparNexus.Domain/
COPY VyaaparNexus.Application/VyaaparNexus.Application.csproj VyaaparNexus.Application/
COPY VyaaparNexus.Infrastructure/VyaaparNexus.Infrastructure.csproj VyaaparNexus.Infrastructure/
COPY VyaaparNexus.API/VyaaparNexus.API.csproj                 VyaaparNexus.API/

RUN dotnet restore VyaaparNexus.API/VyaaparNexus.API.csproj

# Copy remaining source and publish
COPY . .
RUN dotnet publish VyaaparNexus.API/VyaaparNexus.API.csproj \
    --no-restore \
    -c Release \
    -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# mcr.microsoft.com/dotnet/aspnet:8.0 ships the exact shared framework
# that ASP.NET Core 8 expects — including IMeterFactory in
# Microsoft.Extensions.Diagnostics.Abstractions. This eliminates the
# TypeLoadException that occurs when the app is run against the Nix
# dotnet-sdk_8 runtime (which ships a mismatched assembly version).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Railway injects PORT; ASP.NET Core reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VyaaparNexus.API.dll"]
