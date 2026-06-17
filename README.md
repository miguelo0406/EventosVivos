# EventosVivos

Sistema de reservas para eventos culturales, conferencias y talleres. Prueba técnica
fullstack (.NET + Angular) que resuelve control de aforo en tiempo real, conflictos de
horario de venues y validación de reservas/pagos.

**Estado actual**: backend completo y funcional. Frontend Angular pendiente
(próxima fase). Despliegue en AWS pendiente de documentar.

## Tecnologías utilizadas

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 / C# 14, ASP.NET Core Web API |
| Base de datos | PostgreSQL 16 (Docker) + Entity Framework Core (Npgsql) |
| Testing | xUnit, dobles de prueba en memoria (sin mocking framework) |
| Documentación de API | Swagger / OpenAPI (Swashbuckle) |
| Contenedores | Docker + Docker Compose |
| Frontend | Angular (pendiente) |

## Cómo ejecutar el proyecto localmente

### Opción recomendada: Docker Compose (API + PostgreSQL)

Requisitos: Docker Desktop (o compatible) instalado y corriendo.

```bash
# Desde la raíz del repositorio
cp .env.example .env   # opcional: ajustar puertos/credenciales si ya usas 5433 u 8081
docker compose up --build -d
```

Esto levanta dos contenedores:

- `eventosvivos-db`: PostgreSQL 16, puerto host `5433` (configurable con `POSTGRES_PORT`).
- `eventosvivos-api`: la API, puerto host `8081` (configurable con `API_PORT`). Al
  iniciar aplica las migraciones de EF Core automáticamente y siembra los 3 venues de
  referencia (no requiere ningún paso manual adicional).

Una vez arriba:

- Swagger UI: http://localhost:8081/swagger
- API: http://localhost:8081/api/...

Para ver logs: `docker compose logs -f api`. Para detener: `docker compose down`
(agregar `-v` para borrar también los datos de PostgreSQL).

### Opción alternativa: ejecutar la API directamente con el SDK de .NET

Requiere .NET 10 SDK y una instancia de PostgreSQL accesible (puede ser la del
`docker compose` anterior, dejando solo el servicio `db` arriba: `docker compose up db -d`).

```bash
cd EventosVivos.back/EventosVivos
dotnet restore
dotnet run --project src/EventosVivos.Api
```

La cadena de conexión de desarrollo (`appsettings.Development.json`) apunta a
`localhost:5433`, la misma base que expone Docker Compose.

### Ejecutar las pruebas

```bash
cd EventosVivos.back/EventosVivos
dotnet test
```

73 pruebas (56 de dominio, 17 de casos de uso de aplicación) cubriendo las 7 reglas de
negocio (RN-01 a RN-07) y los 6 requerimientos funcionales (RF-01 a RF-06).

## Arquitectura del backend

**Hexagonal (Ports & Adapters)**, elegida para mantener las reglas de negocio
desacopladas de frameworks, HTTP y base de datos — la lógica de EventosVivos debería
poder probarse y evolucionar sin arrancar un servidor web ni una base de datos real.

```
EventosVivos.back/EventosVivos/
├── src/
│   ├── EventosVivos.Domain/          Núcleo: entidades (Event, Reservation, Venue),
│   │                                  value objects (Email, ConfirmationCode),
│   │                                  servicios de dominio, excepciones tipadas y
│   │                                  los puertos de salida (interfaces de
│   │                                  repositorio). No depende de ninguna otra capa
│   │                                  ni de ningún framework externo.
│   ├── EventosVivos.Application/     Casos de uso (uno por operación: CreateEvent,
│   │                                  ReserveTickets, ConfirmReservationPayment,
│   │                                  CancelReservation, GetOccupancyReport, etc.).
│   │                                  Define los puertos de entrada que consume Api
│   │                                  y orquesta el dominio. Depende solo de Domain.
│   ├── EventosVivos.Infrastructure/  Adaptadores de salida: EF Core + Npgsql,
│   │                                  implementación real de los repositorios
│   │                                  definidos en Domain, migraciones, seed de
│   │                                  venues.
│   └── EventosVivos.Api/             Adaptador de entrada: controllers REST,
│                                      middleware de manejo de errores, Swagger,
│                                      composición de dependencias (Program.cs).
└── tests/
    ├── EventosVivos.Domain.Tests/        Pruebas puras de reglas de negocio.
    └── EventosVivos.Application.Tests/   Pruebas de casos de uso con repositorios
                                            en memoria (dobles de prueba) en vez de
                                            mocks.
```

La regla de dependencia es siempre hacia adentro: Domain no conoce a nadie;
Application conoce solo a Domain; Infrastructure y Api conocen a Application y
Domain, nunca al revés. Eso es **Dependency Inversion** (la D de SOLID) aplicada a
nivel arquitectónico: los casos de uso dependen de interfaces (`IEventRepository`,
`IReservationRepository`, `IClock`...) que Domain define y que Infrastructure
implementa, así la base de datos es un detalle reemplazable, no el centro del diseño.

Otras decisiones de diseño relevantes (con su principio SOLID asociado documentado en
el código fuente, junto a la clase o método donde se aplica):

- **Entidades con factory methods y setters privados** (`Event.Create`,
  `Reservation.Create`): garantizan que nunca exista en memoria un objeto que viole
  sus propias reglas de negocio. El estado solo cambia a través de métodos explícitos
  del dominio (`Cancel`, `ConfirmPayment`).
- **Excepciones de dominio tipadas** (`VenueCapacityExceededException`,
  `LateReservationException`, etc.) en vez de excepciones genéricas: permiten que el
  middleware de la Api traduzca cada una al código HTTP correcto (400/404/409/422) sin
  que los controllers necesiten saber de reglas de negocio.
- **Servicios de dominio sin estado** (`VenueScheduleConflictChecker`,
  `TicketAvailabilityCalculator`, `OccupancyReportCalculator`) para lógica que no
  pertenece a una sola entidad (comparar horarios entre eventos, sumar cupos de varias
  reservas).
- **Puertos pequeños y específicos** (`IEventRepository`, `IVenueRepository`,
  `IReservationRepository`, `IClock`) en vez de una interfaz "god repository":
  Interface Segregation — cada caso de uso depende solo de lo que necesita.

### Reglas de negocio: decisiones de interpretación

El enunciado de la prueba tiene un par de puntos ambiguos o subespecificados. Las
decisiones tomadas (y su justificación) están documentadas en
[CLAUDE.md](CLAUDE.md#decisiones-de-interpretación-spec-ambigua-documentadas-para-el-evaluador),
en particular:

- Cómo se concilia RF-05 con RN-07 (estado `perdida` para reservas confirmadas
  canceladas a menos de 48 h del evento).
- Qué reservas cuentan contra el aforo disponible.
- Cómo se calcula el estado `completado` (RN-06) sin un job en segundo plano.

## Convenciones de código

Documentadas en [CLAUDE.md](CLAUDE.md): idioma de identificadores/comentarios,
casing, uso obligatorio de parámetros nombrados, preferencia por `new()` con
target-typed inference, e indentación. Se aplican de forma consistente en todo el
backend.

## Despliegue en AWS

Pendiente — se documentará paso a paso en esta sección (o en un archivo de
despliegue dedicado) una vez el frontend esté listo para containerizar junto al
backend.
