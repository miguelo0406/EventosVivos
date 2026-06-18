# Despliegue en Azure

EventosVivos está desplegado en **Azure Container Apps**. Las 3 piezas del stack (Keycloak,
API, Front) corren como Container Apps independientes dentro del mismo entorno, con una
base de datos PostgreSQL administrada y los secretos centralizados en Key Vault.

## URLs públicas

| Servicio | URL |
|----------|-----|
| Front | https://front.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |
| API + Swagger | https://api.orangetree-9eda1e0d.eastus2.azurecontainerapps.io |
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

## Reproducir el despliegue desde cero (comandos completos)

Asume Azure CLI autenticado (`az login`) sobre la suscripción correcta.

### Fase 0 — Recursos base

El grupo de recursos, ACR, PostgreSQL, Key Vault y el Container Apps Environment se crearon
desde el **Portal de Azure** (no por CLI) en este despliegue concreto — los comandos `az`
equivalentes, para reproducirlo sin Portal, son:

```bash
az group create -n rg-eventosvivos -l eastus

az acr create -n acreventosvivos -g rg-eventosvivos --sku Basic

# Burstable B1ms (free tier 12 meses) — East US 2 por capacidad regional (East US dio
# "La suscripción no se puede aprovisionar..." al intentarlo ahí)
az postgres flexible-server create -n eventosvivos-pg -g rg-eventosvivos \
  --location eastus2 --sku-name Standard_B1ms --tier Burstable \
  --storage-size 32 --version 16 --public-access 0.0.0.0 \
  --admin-user evadmin --admin-password "<password-seguro>"
az postgres flexible-server db create -s eventosvivos-pg -g rg-eventosvivos -d EventosVivosDb
az postgres flexible-server db create -s eventosvivos-pg -g rg-eventosvivos -d keycloak

az keyvault create -n kv-eventosvivos -g rg-eventosvivos --location eastus2 \
  --enable-rbac-authorization true
# tu propia cuenta necesita el rol de PLANO DE DATOS "Key Vault Secrets Officer" (no
# "Key Vault Data Access Administrator", que es de plano de gestión) antes de poder
# crear secretos — ver "Gotchas" arriba
az keyvault secret set --vault-name kv-eventosvivos --name pg-admin-password --value "<...>"
az keyvault secret set --vault-name kv-eventosvivos --name keycloak-db-password --value "<...>"
az keyvault secret set --vault-name kv-eventosvivos --name keycloak-admin-password --value "<...>"
az keyvault secret set --vault-name kv-eventosvivos --name eventosvivos-db-connection \
  --value "Host=eventosvivos-pg.postgres.database.azure.com;Port=5432;Database=EventosVivosDb;Username=evadmin;Password=<...>;Ssl Mode=Require;Trust Server Certificate=true"
az keyvault secret set --vault-name kv-eventosvivos --name keycloak-client-secret --value "<...>"

az containerapp env create -n cae-eventosvivos -g rg-eventosvivos --location eastus2
# anota el dominio público — se necesita para los 3 Container Apps de las siguientes fases
az containerapp env show -n cae-eventosvivos -g rg-eventosvivos --query properties.defaultDomain -o tsv
```

### Fase 1 — Keycloak

```bash
az acr build --registry acreventosvivos --image eventosvivos-keycloak:latest \
  --file keycloak/Dockerfile .

az containerapp create \
  --name keycloak --resource-group rg-eventosvivos --environment cae-eventosvivos \
  --image acreventosvivos.azurecr.io/eventosvivos-keycloak:latest \
  --target-port 8080 --ingress external \
  --registry-server acreventosvivos.azurecr.io --registry-identity system \
  --system-assigned --min-replicas 1 --max-replicas 1

# RBAC: la identidad administrada del Container App necesita poder leer Key Vault
KC_PRINCIPAL_ID=$(az containerapp show -n keycloak -g rg-eventosvivos --query identity.principalId -o tsv)
KV_ID=$(az keyvault show -n kv-eventosvivos -g rg-eventosvivos --query id -o tsv)
az role assignment create --assignee "$KC_PRINCIPAL_ID" --role "Key Vault Secrets User" --scope "$KV_ID"

# Secretos referenciados (nunca el valor en claro en la definición del Container App)
KC_DB_PW_URI=$(az keyvault secret show --vault-name kv-eventosvivos --name keycloak-db-password --query id -o tsv)
KC_ADMIN_PW_URI=$(az keyvault secret show --vault-name kv-eventosvivos --name keycloak-admin-password --query id -o tsv)
az containerapp secret set --name keycloak --resource-group rg-eventosvivos \
  --secrets \
    kc-db-password="keyvaultref:${KC_DB_PW_URI},identityref:system" \
    kc-admin-password="keyvaultref:${KC_ADMIN_PW_URI},identityref:system"

az containerapp update --name keycloak --resource-group rg-eventosvivos \
  --set-env-vars \
    KC_DB=postgres \
    "KC_DB_URL=jdbc:postgresql://eventosvivos-pg.postgres.database.azure.com:5432/keycloak?sslmode=require" \
    KC_DB_USERNAME=evadmin \
    KC_DB_PASSWORD=secretref:kc-db-password \
    KC_HOSTNAME=https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io \
    KC_PROXY_HEADERS=xforwarded \
    KC_HTTP_ENABLED=true \
    KC_HOSTNAME_STRICT=false \
    KC_HEALTH_ENABLED=true \
    KC_BOOTSTRAP_ADMIN_USERNAME=admin \
    KC_BOOTSTRAP_ADMIN_PASSWORD=secretref:kc-admin-password

# Verificar: debe responder un JSON con el "issuer" correcto
curl -s https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/realms/eventosvivos/.well-known/openid-configuration
```

### Fase 2 — API

```bash
az acr build --registry acreventosvivos --image eventosvivos-api:latest \
  --file EventosVivos.back/EventosVivos/Dockerfile EventosVivos.back/EventosVivos

az containerapp create \
  --name api --resource-group rg-eventosvivos --environment cae-eventosvivos \
  --image acreventosvivos.azurecr.io/eventosvivos-api:latest \
  --target-port 8080 --ingress external \
  --registry-server acreventosvivos.azurecr.io --registry-identity system \
  --system-assigned --min-replicas 1 --max-replicas 1 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Cors__AllowedOrigins__0=https://front.orangetree-9eda1e0d.eastus2.azurecontainerapps.io \
    Keycloak__Authority=https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/realms/eventosvivos \
    Keycloak__BaseUrl=https://keycloak.orangetree-9eda1e0d.eastus2.azurecontainerapps.io \
    Keycloak__Realm=eventosvivos \
    Keycloak__ClientId=eventosvivos-api \
    Keycloak__RequireHttpsMetadata=true

API_PRINCIPAL_ID=$(az containerapp show -n api -g rg-eventosvivos --query identity.principalId -o tsv)
az role assignment create --assignee "$API_PRINCIPAL_ID" --role "Key Vault Secrets User" --scope "$KV_ID"

DB_CONN_URI=$(az keyvault secret show --vault-name kv-eventosvivos --name eventosvivos-db-connection --query id -o tsv)
KC_SECRET_URI=$(az keyvault secret show --vault-name kv-eventosvivos --name keycloak-client-secret --query id -o tsv)
az containerapp secret set --name api --resource-group rg-eventosvivos \
  --secrets \
    db-connection="keyvaultref:${DB_CONN_URI},identityref:system" \
    keycloak-client-secret="keyvaultref:${KC_SECRET_URI},identityref:system"

az containerapp update --name api --resource-group rg-eventosvivos \
  --set-env-vars \
    ConnectionStrings__EventosVivosDatabase=secretref:db-connection \
    Keycloak__ClientSecret=secretref:keycloak-client-secret

# Verificar: 200 y las migraciones de EF Core aplicadas (ver logs)
curl -s -o /dev/null -w "%{http_code}\n" https://api.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/
az containerapp logs show -n api -g rg-eventosvivos --tail 40
```

### Fase 3 — Front

El Front es una SPA estática: la URL real de la API se hornea en el bundle JS **antes** del
build de la imagen (no hay `fileReplacements` en `angular.json` — ver "Gotchas" arriba).

```bash
# hornear la URL real de la API en environment.ts, build, y revertir a localhost
sed -i '' "s#http://localhost:8081/api#https://api.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/api#" \
  EventosVivos.front/eventosvivos-web/src/environments/environment.ts

az acr build --registry acreventosvivos --image eventosvivos-front:latest \
  --file EventosVivos.front/eventosvivos-web/Dockerfile EventosVivos.front/eventosvivos-web

# revertir environment.ts a localhost (el repo no debe quedar apuntando a producción)
git checkout -- EventosVivos.front/eventosvivos-web/src/environments/environment.ts

az containerapp create \
  --name front --resource-group rg-eventosvivos --environment cae-eventosvivos \
  --image acreventosvivos.azurecr.io/eventosvivos-front:latest \
  --target-port 80 --ingress external \
  --registry-server acreventosvivos.azurecr.io --registry-identity system \
  --system-assigned --min-replicas 1 --max-replicas 1

# Verificar: 200 y que el bundle JS NO contenga "localhost:8081"
curl -s -o /dev/null -w "%{http_code}\n" https://front.orangetree-9eda1e0d.eastus2.azurecontainerapps.io/
```

### Fase 4 — CI/CD (OIDC federado para GitHub Actions)

```bash
APP_JSON=$(az ad app create --display-name "eventosvivos-github-actions" --query "{appId:appId, id:id}" -o json)
APP_ID=$(echo "$APP_JSON" | jq -r .appId)
APP_OBJECT_ID=$(echo "$APP_JSON" | jq -r .id)
SP_OBJECT_ID=$(az ad sp create --id "$APP_ID" --query id -o tsv)

# credencial federada: solo confía en push a la rama main de este repo exacto
cat > federated-credential.json <<EOF
{
  "name": "github-actions-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:miguelo0406/EventosVivos:ref:refs/heads/main",
  "description": "GitHub Actions push a main",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF
az ad app federated-credential create --id "$APP_OBJECT_ID" --parameters federated-credential.json
rm federated-credential.json

RG_ID=$(az group show -n rg-eventosvivos --query id -o tsv)
ACR_ID=$(az acr show -n acreventosvivos -g rg-eventosvivos --query id -o tsv)
az role assignment create --assignee-object-id "$SP_OBJECT_ID" --assignee-principal-type ServicePrincipal \
  --role "Contributor" --scope "$RG_ID"
az role assignment create --assignee-object-id "$SP_OBJECT_ID" --assignee-principal-type ServicePrincipal \
  --role "AcrPush" --scope "$ACR_ID"

# variables del repo (no son secretas, son IDs públicos de la identidad federada)
gh variable set AZURE_CLIENT_ID --body "$APP_ID" --repo miguelo0406/EventosVivos
gh variable set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)" --repo miguelo0406/EventosVivos
gh variable set AZURE_SUBSCRIPTION_ID --body "$(az account show --query id -o tsv)" --repo miguelo0406/EventosVivos
```

A partir de aquí, cada `git push` a `main` dispara `.github/workflows/deploy.yml`
(`test-api` → 3 builds en paralelo → `deploy`), sin más pasos manuales.
