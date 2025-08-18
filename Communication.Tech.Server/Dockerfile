FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6061
EXPOSE 6013
EXPOSE 5276

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Communication.Tech.Server/Communication.Tech.Server.csproj", "Communication.Tech.Server/"]
RUN dotnet restore "Communication.Tech.Server/Communication.Tech.Server.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Server"
RUN dotnet build "./Communication.Tech.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./Communication.Tech.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_URLS=http://0.0.0.0:6061;http://0.0.0.0:6013

ENTRYPOINT ["dotnet", "Communication.Tech.Server.dll"]