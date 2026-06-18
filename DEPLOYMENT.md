# Despliegue en Azure

EventosVivos está desplegado en **Azure Container Apps**. Las 3 piezas del stack (Keycloak,
API, Front) corren como Container Apps independientes dentro del mismo entorno, con una
base de datos PostgreSQL administrada y los secretos centralizados en Key Vault.

## URLs públicas

| Servicio | URL |
|----------|-----|
| Front | https://front.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |
| API + Swagger | https://api.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/swagger |
| Keycloak | https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |

---

## Qué se creó en Azure

Todo vive en un único grupo de recursos: **`rg-eventosvivos`**.

| Recurso | Nombre | Tipo / SKU | Región | Para qué |
|---------|--------|------------|--------|----------|
| Resource Group | `rg-eventosvivos` | — | East US | Contenedor lógico de todo lo demás. |
| Container Registry | `acreventosvivos` | Basic | East US | Guarda las 3 imágenes Docker (`eventosvivos-keycloak`, `eventosvivos-api`, `eventosvivos-front`). |
| PostgreSQL Flexible Server | `eventosvivos-pg` | Burstable B1ms (free tier 12 meses) | East US 2 | Aloja **dos** bases en el mismo servidor: `EventosVivosDb` (datos de negocio) y `keycloak` (estado del IdP). Requiere SSL. |
| Key Vault | `kv-eventosvivos` | Standard, modelo RBAC | East US 2 | Centraliza secretos: passwords de Postgres/Keycloak admin, connection string, client secret de Keycloak. |
| Container Apps Environment | `cae-eventosvivos` | Consumption | East US 2 | Entorno compartido por los 3 Container Apps; expone el dominio público `orangetree-9eda1e0d.eastus2.azurecontainerapps.io`. |
| Container App | `keycloak` | puerto 8080 | East US 2 | Imagen custom (`keycloak/Dockerfile`) que hornea el realm `eventosvivos` y arranca con `--import-realm`. |
| Container App | `api` | puerto 8080 | East US 2 | API .NET 10, migra la base con EF Core al arrancar. |
| Container App | `front` | puerto 80 | East US 2 | Angular 22 + nginx; la URL pública de la API queda horneada en el bundle JS en build time. |
| App Registration (Azure AD) | `eventosvivos-github-actions` | OIDC federado | — | Identidad que usa GitHub Actions para autenticarse en Azure sin secretos de larga duración (ver [CI/CD](#cicd-github-actions)). |

**Seguridad de secretos**: ningún Container App tiene contraseñas en variables de entorno en
texto plano. Cada uno tiene una **identidad administrada asignada por el sistema**
(`system-assigned managed identity`) con dos roles RBAC sobre los recursos que necesita:

- **`AcrPull`** sobre `acreventosvivos` → para descargar su propia imagen sin credenciales.
- **`Key Vault Secrets User`** sobre `kv-eventosvivos` → para leer sus secretos vía
  referencia (`--secrets nombre=keyvaultref:<uri>,identityref:system`), nunca el valor en
  claro en la definición del Container App.

---

## Orden de despliegue (por qué importa)

`Keycloak → API → Front`, porque cada uno depende de la URL pública del anterior:

1. **Keycloak** no depende de nadie, solo de Postgres. Necesita conocer su **propia** URL
   pública (`KC_HOSTNAME`) para emitir tokens con el `issuer` correcto.
2. **API** necesita la URL pública de Keycloak (`Keycloak__Authority`) para validar JWT.
3. **Front** necesita la URL pública de la API (horneada en el bundle JS en build time, no en
   runtime, porque es una SPA estática servida por nginx) y la API necesita la URL del Front
   para CORS (`Cors__AllowedOrigins__0`).

El `defaultDomain` del Container Apps Environment es predecible **antes** de crear ningún
Container App (`<nombre-app>.<defaultDomain>`), lo que rompe la dependencia circular entre el
CORS de la API y el `apiBaseUrl` del Front: ambas URLs se conocen de antemano aunque los
recursos aún no existan.

---

## Gotchas resueltos durante el despliegue

- **Capacidad regional**: la suscripción no pudo aprovisionar PostgreSQL en East US
  ("La suscripción no se puede aprovisionar..."); se resolvió usando **East US 2** para
  Postgres, Key Vault y el Container Apps Environment (el Resource Group y el ACR quedaron en
  East US sin problema — Azure permite mezclar regiones dentro de un mismo grupo).
- **RBAC de Key Vault**: el modelo de permisos de Key Vault distingue roles de **plano de
  gestión** (`Key Vault Data Access Administrator`, `Owner`, `Contributor` — no dan acceso a
  leer/escribir secretos) de roles de **plano de datos** (`Key Vault Secrets Officer`,
  `Key Vault Secrets User`, `Key Vault Administrator` — estos sí). Asignar el primero por error
  da `RBAC no permite la operación` al intentar crear un secreto.
- **Keycloak detrás de un proxy HTTPS**: `KC_PROXY=edge` está deprecado en Keycloak 26; el
  reemplazo verificado es `KC_PROXY_HEADERS=xforwarded` + `KC_HTTP_ENABLED=true` +
  `KC_HOSTNAME=<url pública>` + `KC_HOSTNAME_STRICT=false`.
- **Import del realm sin bind-mount**: en local, `docker-compose.yml` monta
  `./keycloak/import:/opt/keycloak/data/import:ro`. En la nube no existe el equivalente, así
  que se creó una imagen custom (`keycloak/Dockerfile`) que copia el realm dentro de la imagen
  y arranca con `--import-realm` — idempotente, porque Keycloak persiste el realm importado en
  su propia base Postgres y no lo reimporta en arranques siguientes.
- **`environment.ts` sin `fileReplacements`**: `angular.json` no tiene configurado el
  reemplazo de archivo de entorno por configuración, así que `environment.production.ts` es
  código muerto. La URL real de la API se hornea con `sed` sobre `environment.ts` **antes** del
  build de la imagen del Front (tanto en el despliegue manual como en el workflow de CI/CD).

---

## CI/CD (GitHub Actions)

Workflow: [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml). Se dispara en cada
**push a `main`** (y manualmente con `workflow_dispatch`).

```
push a main
   │
   ├─ test-api ─────────────────────────────┐
   ├─ build-keycloak ─┐                      │
   ├─ build-api ───────┼─ (en paralelo)      │
   ├─ build-front ─────┘                      │
   │                                          │
   └────────────────► deploy (needs: los 4 anteriores) ──► actualiza los 3 Container Apps
```

- **`test-api`**: corre `dotnet test` sobre toda la solución del backend (83 pruebas: 66 de
  dominio + 17 de aplicación). **Si una sola prueba falla, el job `deploy` nunca se ejecuta**
  — GitHub Actions no dispara un job cuyas dependencias (`needs`) no se completaron con éxito.
  Esto es justamente lo que evita desplegar un cambio que rompe una regla de negocio.
- **`build-keycloak` / `build-api` / `build-front`**: construyen cada imagen con
  `az acr build` (build remoto en ACR, no requiere Docker local en el runner) y la etiquetan
  con `:latest` y con el SHA del commit (`:<github.sha>`) para poder rastrear/revertir.
- **`deploy`**: solo si los 4 jobs anteriores fueron exitosos, actualiza los 3 Container Apps
  (`az containerapp update --image ...:<github.sha>`) con la imagen recién construida.

**Autenticación sin secretos de larga duración**: el workflow usa OIDC federado
(`azure/login@v2` + un App Registration en Azure AD, `eventosvivos-github-actions`, con una
credencial federada que confía en `https://token.actions.githubusercontent.com` para el
`subject` `repo:miguelo0406/EventosVivos:ref:refs/heads/main`). No hay ningún client secret de
Azure guardado en GitHub — el runner cambia el token OIDC de GitHub por un token de Azure AD en
tiempo real, válido solo para esa ejecución.

**Variables del repositorio** (GitHub → Settings → Secrets and variables → Actions →
Variables), no son secretas, son IDs públicos de la identidad federada:

| Variable | Valor |
|----------|-------|
| `AZURE_CLIENT_ID` | App ID de `eventosvivos-github-actions` |
| `AZURE_TENANT_ID` | Tenant de Azure AD |
| `AZURE_SUBSCRIPTION_ID` | Suscripción donde vive `rg-eventosvivos` |

**Permisos del Service Principal**: `Contributor` sobre `rg-eventosvivos` (para actualizar los
Container Apps) + `AcrPush` sobre `acreventosvivos` (para subir imágenes).

---

## Reproducir el despliegue desde cero (resumen de comandos)

Asume Azure CLI autenticado (`az login`) y un grupo de recursos ya creado.

```bash
# Registry
az acr create -n acreventosvivos -g rg-eventosvivos --sku Basic

# Postgres (Burstable B1ms, free tier) — ajustar región si hay error de capacidad
az postgres flexible-server create -n eventosvivos-pg -g rg-eventosvivos \
  --location eastus2 --sku-name Standard_B1ms --tier Burstable \
  --storage-size 32 --version 16 --public-access 0.0.0.0
az postgres flexible-server db create -s eventosvivos-pg -g rg-eventosvivos -d EventosVivosDb
az postgres flexible-server db create -s eventosvivos-pg -g rg-eventosvivos -d keycloak

# Key Vault (modelo RBAC) + secretos
az keyvault create -n kv-eventosvivos -g rg-eventosvivos --location eastus2 --enable-rbac-authorization true
# asignar a tu propia cuenta el rol "Key Vault Secrets Officer" antes de poder crear secretos
az keyvault secret set --vault-name kv-eventosvivos --name eventosvivos-db-connection --value "..."
az keyvault secret set --vault-name kv-eventosvivos --name keycloak-client-secret --value "..."

# Container Apps Environment
az containerapp env create -n cae-eventosvivos -g rg-eventosvivos --location eastus2

# Build de las 3 imágenes (build remoto, sin Docker local)
az acr build --registry acreventosvivos --image eventosvivos-keycloak:latest --file keycloak/Dockerfile .
az acr build --registry acreventosvivos --image eventosvivos-api:latest --file EventosVivos.back/EventosVivos/Dockerfile EventosVivos.back/EventosVivos
# (el Front necesita la URL real de la API horneada en environment.ts antes de este build)
az acr build --registry acreventosvivos --image eventosvivos-front:latest --file EventosVivos.front/eventosvivos-web/Dockerfile EventosVivos.front/eventosvivos-web

# Container Apps (con identidad administrada para ACR pull + Key Vault)
az containerapp create -n keycloak -g rg-eventosvivos --environment cae-eventosvivos \
  --image acreventosvivos.azurecr.io/eventosvivos-keycloak:latest \
  --target-port 8080 --ingress external --registry-server acreventosvivos.azurecr.io \
  --registry-identity system --system-assigned
# (api y front siguen el mismo patrón, con ConnectionStrings/Keycloak__* como secretref a Key Vault)
```

Para el flujo completo paso a paso (incluyendo los roles RBAC exactos y la configuración de
cada Container App), ver el historial de comandos `az` ejecutados en este repositorio o
contactar al autor.
