ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY global.json ./
COPY src/Oficina.Cadastro.Domain/Oficina.Cadastro.Domain.csproj src/Oficina.Cadastro.Domain/
COPY src/Oficina.Cadastro.Application/Oficina.Cadastro.Application.csproj src/Oficina.Cadastro.Application/
COPY src/Oficina.Cadastro.Infrastructure/Oficina.Cadastro.Infrastructure.csproj src/Oficina.Cadastro.Infrastructure/
COPY src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj src/Oficina.Cadastro.Api/
RUN dotnet restore src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj

COPY . .
RUN dotnet publish src/Oficina.Cadastro.Api/Oficina.Cadastro.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Oficina.Cadastro.Api.dll"]
