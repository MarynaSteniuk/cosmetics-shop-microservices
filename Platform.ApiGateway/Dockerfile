# API Gateway (YARP + JWT). Збірка з кореня репозиторію:
#   docker build -f Platform.ApiGateway/Dockerfile -t cosmetics-gateway .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Platform.ServiceDefaults/Platform.ServiceDefaults.csproj Platform.ServiceDefaults/
COPY Platform.ApiGateway/Platform.ApiGateway.csproj Platform.ApiGateway/
RUN dotnet restore Platform.ApiGateway/Platform.ApiGateway.csproj
COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.ApiGateway/ Platform.ApiGateway/
RUN dotnet publish Platform.ApiGateway/Platform.ApiGateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Platform.ApiGateway.dll"]
