# syntax=docker/dockerfile:1

# Common Docker file for all AGV projects
# Usage: docker build --build-arg PROJECT=<project> [OPTIONS] PATH
#        project      Project name (e.g. DeviceCommunication)


ARG PROJECT


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
ARG PROJECT
WORKDIR /app

# Copy project files
COPY SharedProjectSettings.csproj ./
COPY Common ./Common/
COPY ${PROJECT} ./${PROJECT}/

# Restore
RUN dotnet restore ${PROJECT}/${PROJECT}.csproj

# Build/publish
RUN dotnet publish -c Release -o out --no-restore ${PROJECT}/${PROJECT}.csproj


# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
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
CMD dotnet /app/Gebhardt.StoreWare.Agv.$PROJECT.dll