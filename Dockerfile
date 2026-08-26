FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Sofistike.slnx
RUN dotnet publish src/Sofistike.Api/Sofistike.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
RUN mkdir -p /app/.keys && chown "$APP_UID:$APP_UID" /app/.keys
USER $APP_UID

ENTRYPOINT ["dotnet", "Sofistike.Api.dll"]
