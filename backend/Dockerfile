FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HelpDesk.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish HelpDesk.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /data && chown -R app:app /app /data
EXPOSE 8080

COPY --from=build /app/publish ./
USER app
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet HelpDesk.Api.dll"]
