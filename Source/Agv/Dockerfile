# syntax=docker/dockerfile:1

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

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build-env
ARG PROJECT
WORKDIR /app

# Copy centralized package management files
COPY Directory.Packages.props ./
COPY Directory.Build.props ./

# Copy Agv project files
COPY Agv/ ./Agv/

# Restore with NuGet authentication (uses BuildKit secret)
RUN --mount=type=secret,id=nuget_pat \
    if [ -f /run/secrets/nuget_pat ]; then \
        dotnet nuget update source AgvDev \
            --username "az" \
            --password "$(cat /run/secrets/nuget_pat)" \
            --store-password-in-clear-text \
            --configfile ./Agv/NuGet.Config; \
    fi && \
    dotnet restore Agv/${PROJECT}/${PROJECT}.csproj

# Build/publish
RUN dotnet publish -c Release -o out --no-restore Agv/${PROJECT}/${PROJECT}.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}

ARG PROJECT
ENV PROJECT=${PROJECT}
WORKDIR /app

RUN apt update &&  \
    apt install -y wget && \
    rm -rf /var/lib/apt/lists/*
EXPOSE 80/tcp
HEALTHCHECK --interval=1m --timeout=10s \
    CMD wget --spider -q --tries=1 -O- http://localhost:80/metrics || exit 1
COPY --from=build-env /app/out ./
CMD ["sh", "-c", "dotnet /app/Gebhardt.Acc.$PROJECT.dll"]
