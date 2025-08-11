FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Communication.Tech.Consumer/Communication.Tech.Consumer.csproj", "Communication.Tech.Consumer/"]
RUN dotnet restore "Communication.Tech.Consumer/Communication.Tech.Consumer.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Consumer"
RUN dotnet build "./Communication.Tech.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Communication.Tech.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Communication.Tech.Consumer.dll"]