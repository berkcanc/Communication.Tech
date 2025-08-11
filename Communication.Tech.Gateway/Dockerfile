FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6060

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Communication.Tech.Gateway/Communication.Tech.Gateway.csproj", "Communication.Tech.Gateway/"]
RUN dotnet restore "Communication.Tech.Gateway/Communication.Tech.Gateway.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Gateway"
RUN dotnet build "./Communication.Tech.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Communication.Tech.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Communication.Tech.Gateway.dll"]