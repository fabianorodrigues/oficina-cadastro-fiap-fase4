ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY global.json ./
COPY src/Oficina.Cadastro.Domain/Oficina.Cadastro.Domain.csproj src/Oficina.Cadastro.Domain/
COPY src/Oficina.Cadastro.Application/Oficina.Cadastro.Application.csproj src/Oficina.Cadastro.Application/
COPY src/Oficina.Cadastro.Infrastructure/Oficina.Cadastro.Infrastructure.csproj src/Oficina.Cadastro.Infrastructure/
COPY src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj src/Oficina.Cadastro.Api/
RUN dotnet restore src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj

COPY src ./src
RUN dotnet publish src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Oficina.Cadastro.Api.dll"]
