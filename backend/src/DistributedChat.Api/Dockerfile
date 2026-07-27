# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY backend/src/DistributedChat.Api/DistributedChat.Api.csproj backend/src/DistributedChat.Api/
COPY backend/src/DistributedChat.Application/DistributedChat.Application.csproj backend/src/DistributedChat.Application/
COPY backend/src/DistributedChat.Domain/DistributedChat.Domain.csproj backend/src/DistributedChat.Domain/
COPY backend/src/DistributedChat.Infrastructure/DistributedChat.Infrastructure.csproj backend/src/DistributedChat.Infrastructure/

RUN dotnet restore backend/src/DistributedChat.Api/DistributedChat.Api.csproj

FROM restore AS publish
ARG APP_VERSION=0.0.0-local
COPY . .
RUN dotnet publish backend/src/DistributedChat.Api/DistributedChat.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:Version="${APP_VERSION}" \
    /p:InformationalVersion="${APP_VERSION}" \
    /p:IncludeSourceRevisionInInformationalVersion=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "DistributedChat.Api.dll"]
