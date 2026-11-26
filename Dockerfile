FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props dotnet-tools.json ./
COPY src/Grana.Domain/Grana.Domain.csproj src/Grana.Domain/
COPY src/Grana.Application/Grana.Application.csproj src/Grana.Application/
COPY src/Grana.Infrastructure/Grana.Infrastructure.csproj src/Grana.Infrastructure/
COPY src/Grana.Api/Grana.Api.csproj src/Grana.Api/
RUN dotnet restore src/Grana.Api/Grana.Api.csproj

COPY src/ src/
RUN dotnet publish src/Grana.Api/Grana.Api.csproj --configuration Release --no-restore --output /app/publish

FROM build AS migrations-build
RUN dotnet tool restore
RUN dotnet ef migrations bundle --project src/Grana.Infrastructure --startup-project src/Grana.Api --configuration Release --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations
WORKDIR /app
COPY --from=migrations-build /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Grana.Api.dll"]
