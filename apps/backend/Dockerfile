FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json Directory.Build.props BetterBooking.sln ./
COPY .config/dotnet-tools.json ./.config/dotnet-tools.json
COPY apps/backend/src/App.Domain/App.Domain.csproj apps/backend/src/App.Domain/
COPY apps/backend/src/App.Application/App.Application.csproj apps/backend/src/App.Application/
COPY apps/backend/src/App.Infrastructure/App.Infrastructure.csproj apps/backend/src/App.Infrastructure/
COPY apps/backend/src/App.Api/App.Api.csproj apps/backend/src/App.Api/

RUN dotnet restore apps/backend/src/App.Api/App.Api.csproj

FROM restore AS build
COPY apps/backend/src/ apps/backend/src/
RUN dotnet publish apps/backend/src/App.Api/App.Api.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM restore AS migrations
COPY apps/backend/src/ apps/backend/src/
RUN dotnet tool restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "App.Api.dll"]
