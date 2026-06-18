# EventosVivos

Sistema de reservas para eventos culturales, conferencias y talleres. Prueba técnica
fullstack (**.NET 10 + Angular 22**) que resuelve tres problemas reales de la startup
EventosVivos:

1. **Control de aforo en tiempo real** — saber cuántas entradas quedan en cada instante.
2. **Conflictos de horario** — un mismo venue no puede albergar dos eventos solapados.
3. **Validación de reservas y pagos** — hoy se hace a mano; aquí es lógica de dominio.

Monorepo con dos carpetas independientes en la raíz: `EventosVivos.back` (API) y
`EventosVivos.front` (SPA).

---

## Tecnologías

| Capa | Tecnología |
|------|------------|
| Backend | .NET 10 / C# 14, ASP.NET Core Web API |
| Frontend | Angular 22 (standalone components + signals), SCSS con tokens |
| Base de datos | PostgreSQL 16 + Entity Framework Core (Npgsql) |
| Mensajería interna | MediatR (CQRS: Commands / Queries / Handlers) |
| Seguridad / IdP | Keycloak 26 (OAuth2 / OIDC) + validación JWT en la API |
| Testing | xUnit — 83 tests (66 de dominio + 17 de aplicación), dobles en memoria sin mocking framework |
| Documentación de API | Swagger / OpenAPI (Swashbuckle) |
| Contenedores | Docker + Docker Compose (`db`, `keycloak`, `api`, `front`) |

---

## Cómo ejecutar

### Opción recomendada: Docker Compose (todo el stack, en cualquier equipo)

Esto es lo único que hace falta para levantar el proyecto completo en una máquina nueva (no
requiere tener .NET ni Node instalados — todo se construye dentro de contenedores).

**Herramienta**: Docker Desktop (Windows/Mac) o Docker Engine + plugin Compose (Linux), en
ejecución.

**Directorio**: la **raíz del repo** (donde está `docker-compose.yml`).

```bash
git clone git@github.com:miguelo0406/EventosVivos.git
cd EventosVivos
cp .env.example .env      # opcional: ajustar puertos/credenciales
docker compose up --build -d
```

`--build` reconstruye las imágenes de `api` y `front` desde su Dockerfile (en vez de buscar una
imagen ya publicada), así que este mismo comando sirve para "subir el aplicativo a Docker desde
cualquier equipo y correrlo desde allí": clona, copia el `.env`, y `docker compose up --build -d`
construye y levanta los 4 servicios sin más prerequisitos que Docker. Levanta cuatro servicios.
Las migraciones de EF Core se aplican solas al arrancar la API y se siembran los 3 venues de
referencia — no hay paso manual.

| Servicio | URL | Notas |
|----------|-----|-------|
| Frontend | http://localhost:4200 | SPA Angular servida por nginx |
| API + Swagger | http://localhost:8081/swagger | REST bajo `/api/...` |
| Keycloak | http://localhost:8088 | Consola admin: `admin` / `admin` |
| PostgreSQL | `localhost:5433` | Base `EventosVivosDb` |

> Puertos no estándar a propósito (5433 / 8081 / 8088), para no chocar con un Postgres o
> Keycloak ya instalados en los puertos por defecto (5432 / 8080).

Logs: `docker compose logs -f api`. Detener: `docker compose down` (`-v` borra también los datos).
Reconstruir solo una imagen tras un cambio: `docker compose up --build -d api` (o `front`).

### Frontend en modo desarrollo (hot reload + debug)

El front necesita el resto del stack (API, Keycloak, DB) corriendo de fondo vía Docker — solo
`ng serve` se reemplaza por el de tu máquina:

```bash
docker compose up -d db keycloak api    # deja el front fuera, lo correrás tú localmente
cd EventosVivos.front/eventosvivos-web
npm install
npm start                                # ng serve → http://localhost:4300
```

**Herramienta**: cualquier IDE con soporte de TypeScript/Angular (VS Code recomendado, con la
extensión Angular Language Service para autocompletado y breakpoints en `.ts`).

Angular 22 exige **Node 20.19+ o 22.12+**. Dos escenarios según lo que tengas instalado:

- **Con nvm** (como en esta máquina): `nvm use 22` antes de `npm install` — el repo no fija un
  `.nvmrc`, así que cualquier Node 20.19+/22.12+ que tengas activo sirve.
- **Con solo Node instalado (sin nvm)**: corré `node -v` primero. Si es ≥ 20.19, no hace falta
  nada más. Si es menor, instalá Node 22 LTS desde https://nodejs.org/ (instalador oficial,
  sin nvm) y reiniciá la terminal antes de `npm install`.

No hace falta instalar Angular CLI global: `npm start` usa el `@angular/cli` local del
`package.json` (`devDependencies`) vía el script `ng serve` de npm.

### Backend en modo desarrollo (debug)

El API necesita Postgres y Keycloak corriendo de fondo, y dos secretos que **no** están en
`appsettings.json` (la connection string y el client secret de Keycloak):

```bash
docker compose up -d db keycloak        # deja la api fuera, la correrás tú localmente/IDE
cd EventosVivos.back/EventosVivos
dotnet user-secrets set "ConnectionStrings:EventosVivosDatabase" \
  "Host=localhost;Port=5433;Database=EventosVivosDb;Username=postgres;Password=postgres" \
  --project src/EventosVivos.Api
dotnet user-secrets set "Keycloak:ClientSecret" "eventosvivos-api-secret-dev" \
  --project src/EventosVivos.Api
```

A partir de ahí, dos formas equivalentes de correrlo con debugger:

- **Desde el IDE** (Visual Studio, Rider, VS Code con C# Dev Kit): abrir
  `EventosVivos.back/EventosVivos/EventosVivos.sln` (o la carpeta) y usar el botón de
  ejecutar/depurar (▶) sobre el proyecto `EventosVivos.Api` — pone breakpoints en cualquier
  capa (Domain/Application/Infrastructure/Api) sin configuración adicional.
- **Desde la terminal**: `dotnet run --project src/EventosVivos.Api` (sin debugger interactivo,
  útil para ver logs en vivo). API en `http://localhost:5000` o similar (puerto que asigne
  `launchSettings.json`); ajustar `apiBaseUrl` del front si se usa contra esta instancia en vez
  de la de Docker (puerto 8081).

`appsettings.Development.json` ya apunta a `http://localhost:8088` (Keycloak vía Docker), así
que no hace falta tocar nada más para que el login funcione localmente.

### Tests del backend

```bash
cd EventosVivos.back/EventosVivos
dotnet test               # 83 tests: 66 dominio + 17 aplicación
```

---

## Cuentas demo

Sembradas en el realm de Keycloak (`keycloak/import/eventosvivos-realm.json`). **Password
para ambas: `Passw0rd!`**

| Email | Rol | Qué puede hacer |
|-------|-----|-----------------|
| `organizador@eventosvivos.com` | **organizer** | Todo lo de asistente + crear/cancelar eventos, ver reporte de ocupación, listar reservas de un evento y administrar venues (CRUD). |
| `asistente@eventosvivos.com` | **attendee** | Ver catálogo, reservar entradas, confirmar/cancelar su reserva, consultar venues. |

El registro propio (`/register`) crea siempre usuarios con rol **attendee**. La consola de
Keycloak (`http://localhost:8088`, `admin`/`admin`) permite promover a `organizer`.

---

## Requisitos cubiertos

### Requerimientos funcionales

Enunciados tal como los plantea la prueba técnica:

| RF | Enunciado | Estado |
|----|-----------|--------|
| RF-01 | Crear evento: título (5-100), descripción (10-500), venue preexistente, capacidad (entero positivo ≤ capacidad del venue), inicio futuro, fin > inicio, precio decimal positivo, tipo (conferencia/taller/concierto). | ✔ back + front |
| RF-02 | Listar eventos con filtros opcionales: tipo, rango de fecha de inicio, venue, estado (activo/cancelado/completado), búsqueda parcial case-insensitive por título. | ✔ back + front |
| RF-03 | Reservar entradas: eventoId, cantidad, nombre y email del comprador; validar disponibilidad, email válido, cantidad ≥ 1; crea reserva en `pendiente_pago`. Regla: si el evento inicia en menos de 24 h, máximo 5 entradas por transacción. | ✔ back + front |
| RF-04 | Confirmar pago: `pendiente_pago` → `confirmada`; genera código único `EV-{6 dígitos}`; rechaza si ya está confirmada o cancelada. | ✔ back + front |
| RF-05 | Cancelar reserva: libera entradas, registra fecha/hora; rechaza con error apropiado según estado. | ✔ back + front |
| RF-06 | Reporte de ocupación por evento: vendidas (confirmadas), disponibles, % ocupación, ingresos (precio × confirmadas), estado del evento. | ✔ back + front |

### Reglas de negocio

| RN | Enunciado | Dónde vive |
|----|-----------|-----------|
| RN-01 | Un evento no puede exceder la capacidad del venue. | `Event.Create` |
| RN-02 | Dos eventos activos no pueden compartir venue con horarios superpuestos. | `VenueScheduleConflictChecker` |
| RN-03 | Eventos en fin de semana (sábado/domingo) no pueden iniciar a las 22:00 o después. | `Event.Create` |
| RN-04 | No se permiten reservas para eventos que inicien en menos de 1 hora. | `Reservation.Create` |
| RN-05 | Eventos con precio > $100 limitan a máximo 10 entradas por transacción. | `Reservation.GetTransactionTicketLimit` |
| RN-06 | Un evento se marca `completado` automáticamente cuando ahora > hora de fin (estado **derivado**, no persistido). | `Event.GetEffectiveStatus` |
| RN-07 | Cancelar una reserva `confirmada` con menos de 48 h del evento la marca como `perdida`: no libera entradas, solo queda para reporte. | `Reservation.Cancel` |

> Ver [Decisiones de diseño / interpretación](#decisiones-de-diseño--interpretación) más abajo
> para los casos donde el enunciado es ambiguo o internamente inconsistente (p. ej. RF-05 vs
> RN-07) y cómo se resolvieron.

---

## Arquitectura del backend — Hexagonal (Ports & Adapters) + CQRS

El dominio no depende de frameworks ni de infraestructura: las flechas de dependencia
apuntan siempre **hacia adentro**.

```
EventosVivos.back/EventosVivos/src/
├── EventosVivos.Domain/          Núcleo. Entidades (Event, Reservation, Venue, User),
│                                  value objects (Email, ConfirmationCode), servicios de
│                                  dominio, excepciones tipadas, puertos de salida
│                                  (IXxxRepository, IClock). No depende de nadie.
├── EventosVivos.Application/      Casos de uso. CQRS: Commands/Queries → Handlers delgados
│                                  → Services/Facades que orquestan dominio + puertos.
│                                  DTOs, mapeo, puerto de entrada IIdentityProvider.
│                                  Depende solo de Domain.
├── EventosVivos.Infrastructure/   Adaptadores de salida: repositorios EF Core + PostgreSQL,
│                                  KeycloakIdentityProvider, SystemClock. Depende de
│                                  Domain y Application.
└── EventosVivos.Api/              Adaptador de entrada: controllers REST delgados (HTTP →
                                   IMediator), middleware de errores, JWT, DI (Program.cs).
```

**Flujo de una petición:** `Controller delgado → IMediator → Handler delgado →
Service/Facade → Dominio + Repos`. Toda respuesta (éxito y error) va envuelta en el
envelope uniforme `ApiResponse<T>` = `{ ok, data, error, requestId, timestamp }`.

---

## Arquitectura del frontend (Angular 22)

App en `EventosVivos.front/eventosvivos-web`. Standalone components + signals, lazy loading
por ruta. La estructura **refleja el hexagonal** del backend:

| Carpeta del front | Rol | Equivalente en el back |
|-------------------|-----|------------------------|
| `core/models` | DTOs de negocio | Domain (contratos) |
| `core/services` | Adaptadores REST (un servicio por recurso) | Repositorios |
| `features/*` | Componentes (pantallas) | Casos de uso |
| `app.config.ts` | Composition root (DI, HttpClient, interceptor) | Program.cs |
| `core/auth/auth.interceptor.ts` | Adjunta JWT + renueva sesión | Middleware |

Dos superficies sobre el mismo sistema de diseño (`styles.scss`, tokens oklch):

- **Asistente**: login/registro, catálogo con filtros, detalle de evento + flujo de reserva (RF-03..05).
- **Organizador** (`features/organizer/`, gateado por `organizerGuard`): panel `/admin`,
  crear evento, gestionar evento (ocupación + reservas + cancelar) y CRUD de venues.

---

## Patrones de diseño aplicados

| Patrón | Dónde | Para qué |
|--------|-------|----------|
| **Ports & Adapters (Hexagonal)** | Puertos `IXxxRepository`, `IClock`, `IIdentityProvider` en Domain/Application; adaptadores en Infrastructure y Api | Aislar el negocio de la tecnología; el motor de BD o el IdP son intercambiables. |
| **CQRS + Mediator** | `Commands/`, `Queries/` y sus Handlers vía MediatR | Separar intención (lectura/escritura) de la orquestación; controllers que no conocen los servicios concretos. |
| **Factory Method** | `Event.Create`, `Venue.Create`, `Reservation.Create` (constructor privado) | Garantizar por construcción que nunca exista una entidad que viole sus invariantes. |
| **Facade** | `IEventService`, `IReservationService`, `IVenueService`, `IAuthService` | Concentrar la orquestación de cada agregado tras una interfaz delgada. |
| **Value Object** | `Email`, `ConfirmationCode` (inmutables) | Encapsular invariantes de valor (formato de email, formato `EV-######`). |
| **Domain Service** | `VenueScheduleConflictChecker`, `OccupancyReportCalculator`, `TicketAvailabilityCalculator` | Lógica de negocio que no pertenece a una sola entidad. |
| **Adapter (front)** | `core/services/*.service.ts` traducen el envelope `ApiResponse<T>` | Los componentes dependen de un servicio inyectado, no de `HttpClient`. |
| **Facade (front)** | `AuthService` (tokens, refresh proactivo, logout por inactividad) | Una sola fachada para todo el ciclo de sesión. |
| **Interceptor / Chain of Responsibility (front)** | `auth.interceptor.ts` | Adjunta el Bearer y reintenta tras refrescar en 401, transparente a los servicios. |
| **Guard / Strategy (front)** | `authGuard`, `organizerGuard` en `app.routes.ts` | Estrategia de activación de ruta según sesión/rol. |
| **Observer (front)** | signals + RxJS | UI reactiva al estado. |

---

## Principios SOLID (con ubicación en el código)

- **S — Responsabilidad única**: cada clase tiene una sola razón de cambio.
  `ExceptionHandlingMiddleware` (único punto que decide el HTTP), los servicios de
  aplicación (`EventService`, `ReservationService`, `VenueService`, `AuthService`),
  `VenueScheduleConflictChecker` (solo compara intervalos), `TicketAvailabilityCalculator`,
  `Email`, `ConfirmationCode`, `SystemClock`.
- **O — Abierto/Cerrado**: `OccupancyReportCalculator` se extiende con nuevas métricas sin
  modificar su núcleo de cálculo.
- **L — Sustitución de Liskov**: `IClock` (SystemClock ↔ FixedClock en tests) e
  `IEventRepository` (EF Core ↔ `InMemoryEventRepository`) son intercambiables sin que el
  caso de uso lo note.
- **I — Segregación de interfaces**: repositorios separados (`IEventRepository`,
  `IVenueRepository`, `IReservationRepository`) en vez de un "god repository"; cada handler
  recibe solo el servicio que necesita.
- **D — Inversión de dependencias**: el núcleo define los contratos (`IEventRepository`,
  `IIdentityProvider`) y la infraestructura (EF Core, Keycloak) se adapta a ellos, nunca al
  revés. En el front, los componentes dependen de servicios inyectados, no de `HttpClient`.

> Convención del repo: cada vez que un principio SOLID se aplica de forma deliberada, hay un
> comentario breve en español en el código indicando cuál y por qué.

---

## Seguridad (valor agregado)

> El enunciado **no** exige autenticación; se implementó para cubrir el criterio
> "Seguridad de la aplicación".

- **Keycloak 26** como IdP (realm `eventosvivos`, cliente confidencial `eventosvivos-api`).
- **Auth proxied por la API**: el navegador nunca habla directo con Keycloak; usa
  `/api/auth/register|login|refresh` (CQRS → `AuthService` → `IIdentityProvider` /
  `KeycloakIdentityProvider`). Las pantallas de login/registro son propias (Angular).
- **JWT stateless**: la API valida firma/emisor/vigencia en cada request. Una
  `FallbackPolicy = RequireAuthenticatedUser` protege todo salvo `/api/auth/*`.
- **Roles** (`attendee` / `organizer`): el JWT trae `realm_access.roles`; la API los aplana
  a claims y define la policy `Organizer`. Los endpoints de gestión (crear/cancelar evento,
  reporte de ocupación, reservas de un evento, CRUD de venues) exigen `organizer`.
- **Sesión**: access token corto (5 min) + refresh; logout por inactividad de 5 min.
- **Espejo local**: tabla `users(Id, email, keycloak_subject_id)` con provisioning JIT.

---

## Manejo de errores

Excepciones de dominio/aplicación **tipadas** (no genéricas) + un único
`ExceptionHandlingMiddleware` que las mapea a HTTP y al envelope:

| HTTP | Significado | Ejemplos |
|------|-------------|----------|
| 400 | Validación de forma | `DomainValidationException` |
| 401 / 403 | No autenticado / sin rol | JWT inválido / falta `organizer` |
| 404 | No encontrado | evento/reserva/venue inexistente |
| 409 | Conflicto de invariante | `VENUE_IN_USE`, `VENUE_CAPACITY_BELOW_SCHEDULED_EVENT`, horario solapado |
| 422 | Regla de negocio | `WEEKEND_NIGHT_RESTRICTION`, `LATE_RESERVATION`, `TICKET_LIMIT_EXCEEDED` |

---

## Decisiones de diseño / interpretación

- **RF-05 vs RN-07** (spec internamente inconsistente): una reserva se cancela desde
  `pendiente_pago` o `confirmada`; solo se rechaza si ya es terminal. Se modela un cuarto
  estado **`perdida`** (distinto de `cancelada`): "cancelada sin liberar cupo, solo para
  reporte" (RN-07).
- **Disponibilidad**: cuentan contra el cupo `pendiente_pago`, `confirmada` y `perdida`.
  Solo `cancelada` libera entradas.
- **RN-06 (completado)**: estado **derivado** en consulta (`ahora > fin && activo`), sin job
  en segundo plano — dominio simple y testeable.
- **Hora local en el front, UTC en el back**: el organizador teclea y ve fechas en su hora
  local (el navegador la conoce); `Date.toISOString()` la convierte al UTC real antes de
  enviarla. El backend razona siempre en `_clock.UtcNow` y Postgres usa `timestamptz` — un
  único instante absoluto, sin ambigüedad de zona. RN-03 evalúa la hora local que tecleó el
  organizador (ya convertida a su UTC equivalente), no una hora UTC arbitraria. Las listas y
  el detalle muestran las fechas en la zona horaria de cada visitante, no forzadas a UTC.
- **CRUD de venues (valor agregado)**: el enunciado trata los venues como datos de
  referencia. Se añadió un CRUD de organizador para cubrir "casos borde": no se puede borrar
  un venue con eventos (`VENUE_IN_USE`) ni reducir su capacidad por debajo de un evento ya
  programado (`VENUE_CAPACITY_BELOW_SCHEDULED_EVENT`).
- **Secretos**: la connection string no va en config. Local → .NET User Secrets; Docker →
  variables de entorno vía `.env` (gitignored).

---

## Tests

83 tests con xUnit, sin mocking framework (dobles en memoria que cumplen los mismos puertos):

- **`EventosVivos.Domain.Tests`** (66): invariantes de `Event`, `Reservation`, `Venue`,
  value objects (`Email`, `ConfirmationCode`) y servicios de dominio (conflicto de horario,
  ocupación, disponibilidad). Cubren RN-01..07 y los casos borde de validación.
- **`EventosVivos.Application.Tests`** (17): orquestación de `EventService` y
  `ReservationService` contra repositorios en memoria y un reloj fijo (`FixedClock`).

```bash
cd EventosVivos.back/EventosVivos && dotnet test
```

---

## Despliegue en Azure

El stack está desplegado y accesible públicamente:

| Servicio | URL |
|----------|-----|
| **Front** | https://front.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |
| **API + Swagger** | https://api.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/swagger |
| **Keycloak** | https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |

**Qué se creó**: un grupo de recursos (`rg-eventosvivos`) con Azure Container Registry,
PostgreSQL Flexible Server (dos bases en el mismo servidor: `EventosVivosDb` y `keycloak`),
Key Vault (modelo RBAC, sin secretos en texto plano en ningún Container App) y un Container
Apps Environment con los 3 servicios (`keycloak`, `api`, `front`), cada uno con identidad
administrada para descargar su imagen y leer sus secretos. El detalle completo — por qué ese
orden de despliegue, los gotchas resueltos (capacidad regional, RBAC de Key Vault, Keycloak
detrás de proxy, etc.) y los comandos `az` para reproducirlo — está en
**[`DEPLOYMENT.md`](DEPLOYMENT.md)**.

### CI/CD: despliegue automático en cada push a `main`

Workflow: [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml). Build remoto de las
3 imágenes con `az acr build` (sin Docker local en el runner) + actualización de los 3
Container Apps, autenticado con **OIDC federado** (sin secretos de larga duración guardados en
GitHub: `azure/login@v2` intercambia el token de GitHub Actions por un token de Azure AD en
cada ejecución).

**Gate obligatorio antes de desplegar**: el job `test-api` corre los 83 tests del backend
(`dotnet test`). El job `deploy` declara `needs: [test-api, build-keycloak, build-api,
build-front]` — si una sola prueba falla, GitHub Actions no ejecuta `deploy` y los Container
Apps no se tocan. Así un cambio que rompe una regla de negocio nunca llega a producción.

```
push a main → test-api ┐
            → build-keycloak ┤
            → build-api      ├─► deploy (solo si TODOS los anteriores pasaron)
            → build-front   ┘
```

Artefactos del repo que hacen esto posible:

| Artefacto | Dónde | Qué hace |
|-----------|-------|----------|
| `deploy.yml` | `.github/workflows/` | Define los 5 jobs (test-api + 3 builds + deploy). |
| App Registration `eventosvivos-github-actions` | Azure AD | Identidad federada que GitHub Actions usa vía OIDC; tiene `Contributor` sobre `rg-eventosvivos` y `AcrPush` sobre el ACR. |
| Variables del repo (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`) | GitHub → Settings → Secrets and variables → Actions → Variables | IDs públicos (no secretos) que identifican esa identidad federada ante Azure. |
| `keycloak/Dockerfile` | `keycloak/` | Hornea el realm `eventosvivos` dentro de la imagen (sin esto, el job `build-keycloak` no tendría qué construir). |
</content>
