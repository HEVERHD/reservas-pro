# Despliegue en Azure

| Pieza | Servicio de Azure | Costo |
|---|---|---|
| API | App Service (Linux, plan F1) | Gratis (60 min de CPU al día; suficiente para una demo) |
| Panel | Static Web Apps (plan Free) | Gratis |
| Base de datos | Azure Database for PostgreSQL – Flexible Server (B1ms) | Incluida 12 meses en la cuenta gratuita de Azure; después tiene costo mensual |
| Secretos | Key Vault | Centavos al mes por pocas lecturas |

El workflow [`desplegar.yml`](../.github/workflows/desplegar.yml) prueba, publica la API, comprueba `/health`
y publica el panel. Entra a Azure con **OIDC**: GitHub no guarda ninguna contraseña de Azure.

Los comandos usan la CLI de Azure (`az`). Cambia los nombres (deben ser únicos en Azure) y la región.

## 1. Recursos

```bash
GRUPO=rg-reservas-pro
REGION=eastus2
API=reservas-pro-api-hg
PG=reservas-pro-pg-hg
KV=kv-reservas-pro-hg

az group create -n $GRUPO -l $REGION

# API
az appservice plan create -g $GRUPO -n plan-reservas-pro --is-linux --sku F1
az webapp create -g $GRUPO -p plan-reservas-pro -n $API --runtime "DOTNETCORE:10.0"
az webapp update -g $GRUPO -n $API --https-only true
az webapp identity assign -g $GRUPO -n $API

# Base de datos: la contraseña se genera al azar en una variable de la terminal y en el paso 2 va a Key Vault
PG_USUARIO=reservasadmin
PG_CLAVE=$(openssl rand -base64 24 | tr -d '/+=')
az postgres flexible-server create -g $GRUPO -n $PG -l $REGION \
  --admin-user $PG_USUARIO --admin-password "$PG_CLAVE" \
  --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 17 \
  --database-name reservas --public-access 0.0.0.0   # 0.0.0.0 = solo servicios de Azure

# Panel
az staticwebapp create -g $GRUPO -n reservas-pro-panel -l $REGION --sku Free
```

## 2. Secretos en Key Vault

La API lee la cadena de conexión y la clave de los JWT desde Key Vault mediante *Key Vault references*:
los valores nunca quedan en la configuración del App Service ni en el repositorio.

```bash
az keyvault create -g $GRUPO -n $KV -l $REGION --enable-rbac-authorization true

# Permiso de lectura para la identidad de la API
API_ID=$(az webapp identity show -g $GRUPO -n $API --query principalId -o tsv)
az role assignment create --assignee $API_ID --role "Key Vault Secrets User" \
  --scope $(az keyvault show -n $KV --query id -o tsv)

# Los valores se escriben desde la terminal, sin pegarlos en ningún archivo
az keyvault secret set --vault-name $KV -n jwt-clave --value "$(openssl rand -base64 48)"
az keyvault secret set --vault-name $KV -n conexion-reservas \
  --value "Host=$PG.postgres.database.azure.com;Database=reservas;Username=$PG_USUARIO;Password=$PG_CLAVE;SSL Mode=Require"
unset PG_CLAVE
```

## 3. Configuración de la API

```bash
PANEL_URL=https://$(az staticwebapp show -g $GRUPO -n reservas-pro-panel --query defaultHostname -o tsv)

az webapp config appsettings set -g $GRUPO -n $API --settings \
  "ConnectionStrings__Reservas=@Microsoft.KeyVault(VaultName=$KV;SecretName=conexion-reservas)" \
  "Jwt__Clave=@Microsoft.KeyVault(VaultName=$KV;SecretName=jwt-clave)" \
  "Cors__Origenes__0=$PANEL_URL" \
  "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"
```

`ASPNETCORE_FORWARDEDHEADERS_ENABLED` hace que la API vea la IP real del cliente detrás del proxy de Azure;
sin eso, el límite de reservas y de intentos de login contaría a todos los usuarios como uno solo.

La API aplica las migraciones al arrancar (una sola instancia). Antes del primer despliegue hay que crear
la migración inicial en un entorno donde se pueda ejecutar .NET (Codespaces):

```bash
dotnet ef migrations add Inicial --project src/ReservasPro.Api --output-dir Data/Migraciones
```

## 4. Acceso de GitHub a Azure con OIDC

```bash
APP_ID=$(az ad app create --display-name github-reservas-pro --query appId -o tsv)
az ad sp create --id $APP_ID

# Solo el environment "produccion" de este repositorio puede usar esta identidad
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "github-produccion",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:HEVERHD/reservas-pro:environment:produccion",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Solo puede desplegar esta Web App, nada más de la suscripción
az role assignment create --assignee $APP_ID --role "Website Contributor" \
  --scope $(az webapp show -g $GRUPO -n $API --query id -o tsv)
```

## 5. Configuración en GitHub

En el repositorio: **Settings → Environments → New environment → `produccion`**, y dentro de él:

| Tipo | Nombre | Valor |
|---|---|---|
| Secret | `AZURE_CLIENT_ID` | `$APP_ID` |
| Secret | `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| Secret | `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| Secret | `AZURE_STATIC_WEB_APPS_API_TOKEN` | `az staticwebapp secrets list -n reservas-pro-panel --query properties.apiKey -o tsv` |
| Variable | `AZURE_WEBAPP_NAME` | `$API` |
| Variable | `API_URL` | `https://$API.azurewebsites.net` |

Después: **Actions → Desplegar en Azure → Run workflow**.

## Primer usuario en producción

El negocio y el dueño de ejemplo solo se crean en desarrollo. En producción, el primer negocio y su usuario
se insertan con un script SQL de alta (pendiente) o desde un endpoint de administración protegido.
