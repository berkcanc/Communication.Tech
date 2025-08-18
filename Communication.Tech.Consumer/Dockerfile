FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Communication.Tech.Consumer/Communication.Tech.Consumer.csproj", "Communication.Tech.Consumer/"]
RUN dotnet restore "Communication.Tech.Consumer/Communication.Tech.Consumer.csproj"
COPY . .
WORKDIR "/src/Communication.Tech.Consumer"
RUN dotnet build "./Communication.Tech.Consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./Communication.Tech.Consumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Communication.Tech.Consumer.dll"]