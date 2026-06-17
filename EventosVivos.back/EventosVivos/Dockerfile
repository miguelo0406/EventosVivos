# Etapa 1: build y publish con el SDK completo de .NET 10.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copian primero los .csproj para que Docker cachee la capa de "dotnet restore":
# mientras no cambien las dependencias, los rebuilds posteriores no vuelven a
# restaurar paquetes NuGet, solo recompilan el código que cambió.
COPY EventosVivos.sln ./
COPY src/EventosVivos.Domain/EventosVivos.Domain.csproj src/EventosVivos.Domain/
COPY src/EventosVivos.Application/EventosVivos.Application.csproj src/EventosVivos.Application/
COPY src/EventosVivos.Infrastructure/EventosVivos.Infrastructure.csproj src/EventosVivos.Infrastructure/
COPY src/EventosVivos.Api/EventosVivos.Api.csproj src/EventosVivos.Api/
COPY tests/EventosVivos.Domain.Tests/EventosVivos.Domain.Tests.csproj tests/EventosVivos.Domain.Tests/
COPY tests/EventosVivos.Application.Tests/EventosVivos.Application.Tests.csproj tests/EventosVivos.Application.Tests/

RUN dotnet restore EventosVivos.sln

COPY . .

RUN dotnet publish src/EventosVivos.Api/EventosVivos.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Etapa 2: imagen final, solo el runtime de ASP.NET (mucho más liviana que el SDK,
# no incluye herramientas de compilación en la imagen que corre en producción).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Usuario sin privilegios provisto por la imagen base oficial: evita correr el
# proceso de la API como root dentro del contenedor.
USER $APP_UID

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EventosVivos.Api.dll"]
