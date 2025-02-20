FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Production

# Теперь копируем все оставшиеся файлы и папки проекта
COPY ./ /src/

WORKDIR /src/Host

RUN dotnet restore "Host.csproj"

WORKDIR /src/Host

# Проверка содержимого appsettings.Production.json после установки секретов
RUN dotnet build "Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Production
RUN dotnet tool install --global dotnet-ef
RUN dotnet publish "Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Host.dll"]
