FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Reciclame.Api/Reciclame.Api.csproj", "Reciclame.Api/"]
RUN dotnet restore "Reciclame.Api/Reciclame.Api.csproj"

COPY ["Reciclame.Api/", "Reciclame.Api/"]
RUN dotnet publish "Reciclame.Api/Reciclame.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Reciclame.Api.dll"]
