FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/HelpDesk.Api.csproj ./
RUN dotnet restore

COPY backend/. ./
RUN dotnet publish HelpDesk.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /data
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet HelpDesk.Api.dll"]
