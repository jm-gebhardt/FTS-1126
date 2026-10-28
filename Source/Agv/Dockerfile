# syntax=docker/dockerfile:1.7-labs

# Common Docker file for all AGV projects
# Usage: docker build -f Agv/Dockerfile --build-arg PROJECT=<project> DOTNET_VERSION=<version> [OPTIONS] .
#        Build context must be 'Source/' directory (contains Directory.Packages.props)
#        PROJECT             Project name (e.g. DeviceCommunication)
#        DOTNET_VERSION      .NET version (e.g. 8.0)
#
# Authentication for private NuGet feed:
#   Local:    Set NUGET_PAT environment variable (or in .env file for docker-compose)
#   Pipeline: Uses System.AccessToken automatically via NuGetAuthenticate task

ARG PROJECT
ARG DOTNET_VERSION=10.0

# Restore stage: independent of PROJECT. When all services are built in one 'docker compose build'
# this stage is identical for every image, so BuildKit computes it once and the other 20 reuse it.
# Only project files are copied here, so editing a .cs file does not invalidate the restore layer;
# only a change to a .csproj, the props files, or NuGet.Config does.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS restore
ARG TARGETARCH
WORKDIR /app

# Map Docker TARGETARCH (amd64/arm64) to .NET RID for Alpine
RUN if [ "$TARGETARCH" = "arm64" ]; then echo "linux-musl-arm64" > /tmp/rid; \
    else echo "linux-musl-x64" > /tmp/rid; fi

# Copy only what package resolution needs, preserving directory layout (--parents).
COPY Directory.Packages.props Directory.Build.props ./
COPY --parents Agv/**/*.csproj ./
COPY Agv/SharedProjectsSettings.props Agv/NuGet.Config Agv/CompileAgv.sln ./Agv/

# Restore the whole solution once. The package cache mount is shared across services and across
# runs, so packages are downloaded only when a version actually changes.
RUN --mount=type=secret,id=nuget_pat \
    --mount=type=cache,target=/root/.nuget/packages \
    if [ -f /run/secrets/nuget_pat ]; then \
        dotnet nuget update source feed-AgvDev \
            --username "az" \
            --password "$(cat /run/secrets/nuget_pat)" \
            --store-password-in-clear-text \
            --configfile ./Agv/NuGet.Config; \
    fi && \
    dotnet restore -r $(cat /tmp/rid) Agv/CompileAgv.sln

# Build stage: PROJECT-specific. Brings in the full source and publishes one service.
FROM restore AS build-env
ARG PROJECT
COPY Agv/ ./Agv/

# Build/publish (target Alpine RID to avoid bundling native assets for other platforms)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish -c Release -o out --no-restore -r $(cat /tmp/rid) --self-contained false Agv/${PROJECT}/${PROJECT}.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine

# Install ICU libraries for full globalization support (required by Microsoft.Data.SqlClient)
# Install tzdata so TimeZoneInfo.FindSystemTimeZoneById can resolve IANA ids (e.g. Europe/Berlin)
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ARG PROJECT
ENV PROJECT=${PROJECT}
WORKDIR /app

EXPOSE 80/tcp
HEALTHCHECK --interval=1m --timeout=10s --start-period=60s --start-interval=3s \
    CMD wget --spider -q --tries=1 -O- http://localhost:80/metrics || exit 1
COPY --from=build-env /app/out ./
CMD ["sh", "-c", "dotnet /app/Gebhardt.Acc.$PROJECT.dll"]
