FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5060
EXPOSE 6011
EXPOSE 5273

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Communication.Tech.Server/Communication.Tech.Server.csproj", "Communication.Tech.Server/"]
COPY ["Communication.Tech.Protos/Communication.Tech.Protos.csproj", "Communication.Tech.Protos/"]
RUN dotnet restore "Communication.Tech.Server/Communication.Tech.Server.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Server"
RUN dotnet build "./Communication.Tech.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Communication.Tech.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Communication.Tech.Server.dll"]