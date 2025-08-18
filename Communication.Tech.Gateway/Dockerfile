FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6060

# for curl test
RUN apt-get update && apt-get install -y curl

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Communication.Tech.Gateway/Communication.Tech.Gateway.csproj", "Communication.Tech.Gateway/"]
RUN dotnet restore "Communication.Tech.Gateway/Communication.Tech.Gateway.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Gateway"
RUN dotnet build "./Communication.Tech.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./Communication.Tech.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Communication.Tech.Gateway.dll"]