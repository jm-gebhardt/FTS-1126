# syntax=docker/dockerfile:1

# Common Docker file for all AGV projects
# Usage: docker build -f Agv/Dockerfile --build-arg PROJECT=<project> DOTNET_VERSION=<version> [OPTIONS] .
#        Build context must be 'Source/' directory (contains Directory.Packages.props)
#        PROJECT             Project name (e.g. DeviceCommunication)
#        DOTNET_VERSION      .NET version (e.g. 8.0)

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

# Restore
RUN dotnet restore Agv/${PROJECT}/${PROJECT}.csproj

# Build/publish
RUN dotnet publish -c Release -o out --no-restore Agv/${PROJECT}/${PROJECT}.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}

ENV PROJECT=${PROJECT}
WORKDIR /app

RUN apt update &&  \
    apt install -y wget && \
    rm -rf /var/lib/apt/lists/*
EXPOSE 80/tcp    
HEALTHCHECK --interval=1m --timeout=10s \
    CMD wget --spider -q --tries=1 -O- http://localhost:80/metrics || exit 1
COPY --from=build-env /app/out ./
CMD dotnet /app/Gebhardt.Acc.$PROJECT.dll