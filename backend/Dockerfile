FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# O csproj vai antes do código para o restore ficar em cache entre builds.
COPY CieeCurriculos.Api/CieeCurriculos.Api.csproj CieeCurriculos.Api/
RUN dotnet restore CieeCurriculos.Api/CieeCurriculos.Api.csproj
COPY CieeCurriculos.Api/ CieeCurriculos.Api/
RUN dotnet publish CieeCurriculos.Api/CieeCurriculos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# A imagem aspnet não traz curl, e o healthcheck do compose precisa dele.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CieeCurriculos.Api.dll"]
