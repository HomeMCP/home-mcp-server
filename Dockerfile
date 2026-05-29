# ─────────────────────────────────────────────────────────────────────────────
# HomeMcp.Server — gRPC (Converse) + REST (admin + pairing) backend
# Multi-stage: restore → publish → slim ASP.NET runtime
# ─────────────────────────────────────────────────────────────────────────────

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Solution-level build configuration (central package management + analyzers)
COPY Directory.Build.props Directory.Packages.props .editorconfig ./

# 2. Project manifests first — keeps `restore` in a cached layer until a .csproj changes
COPY src/HomeMcp.Domain/HomeMcp.Domain.csproj                 src/HomeMcp.Domain/
COPY src/HomeMcp.Application/HomeMcp.Application.csproj        src/HomeMcp.Application/
COPY src/HomeMcp.Infrastructure/HomeMcp.Infrastructure.csproj src/HomeMcp.Infrastructure/
COPY src/HomeMcp.Contracts/HomeMcp.Contracts.csproj           src/HomeMcp.Contracts/
COPY src/HomeMcp.Server/HomeMcp.Server.csproj                 src/HomeMcp.Server/
COPY plugins/HomeMcp.Plugin.Jellyfin/HomeMcp.Plugin.Jellyfin.csproj           plugins/HomeMcp.Plugin.Jellyfin/
COPY plugins/HomeMcp.Plugin.HomeAssistant/HomeMcp.Plugin.HomeAssistant.csproj plugins/HomeMcp.Plugin.HomeAssistant/

RUN dotnet restore src/HomeMcp.Server/HomeMcp.Server.csproj

# 3. Full source + publish (self-contained framework-dependent build)
COPY src/ src/
COPY plugins/ plugins/
RUN dotnet publish src/HomeMcp.Server/HomeMcp.Server.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# gRPC + REST on a single Http1AndHttp2 cleartext endpoint (see appsettings.json)
ENV ASPNETCORE_URLS=http://+:5201 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__Sqlite__Path=/app/data/home-mcp.db

EXPOSE 5201

# SQLite database + setup.json live here — mount a volume to persist them
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "HomeMcp.Server.dll"]
