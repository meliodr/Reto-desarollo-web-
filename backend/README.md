# API de planificación estratégica

La API .NET 10 usa PostgreSQL para formular, revisar y publicar PEI y para crear o consultar POA independientes o vinculados. [Configurar Keycloak para el flujo del SRS v2](../docs/plan-sisplan/10-ADAPTACION-SRS-V2.md#verificación-del-incremento).

## Arranque local de F0

Desde la raíz del repositorio, prepara cuatro claves locales distintas. Las siguientes variables duran la sesión de shell. Para reutilizar los volúmenes en otra sesión, conserva sus valores fuera del repositorio.

```sh
export SISPLAN_DB_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_APP_DB_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_KEYCLOAK_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_F0_PASSWORD="$(openssl rand -hex 24)"
docker compose --profile institucional up -d
docker compose up -d --wait postgres
```

En la primera inicialización, PostgreSQL crea `sisplan_app` como login de aplicación. Para un volumen PostgreSQL anterior, ejecuta `docker compose exec -T postgres sh /docker-entrypoint-initdb.d/20-sisplan-app.sh` después de incorporar las variables a Compose. Si el login existe, el script verifica sus privilegios y conserva su contraseña.

```sh
export ASPNETCORE_ENVIRONMENT=Development
export Sisplan__Oidc__Url=http://127.0.0.1:8080
export ConnectionStrings__SisplanMigraciones="Host=127.0.0.1;Port=55432;Database=sisplan;Username=sisplan;Password=${SISPLAN_DB_PASSWORD}"
export ConnectionStrings__Sisplan="Host=127.0.0.1;Port=55432;Database=sisplan;Username=sisplan_app;Password=${SISPLAN_APP_DB_PASSWORD}"
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile -- --migrate
```

La cuenta de migración posee el esquema. El login de la API queda sujeto a las políticas por fila. La migración registra una huella por versión. Puedes ejecutar `--migrate` otra vez para verificarla. **Para actualizar una base con planes previos**, ejecuta primero [la preparación de correspondencias](../infra/postgres/preparar-historicos.sql), registra la institución destinataria de cada PEI y cada POA independiente y luego aplica la migración. Los POA vinculados heredan la institución del PEI.

### Identidades y datos de demostración

La carga de pruebas se ejecuta en una base local desechable. Espera a que la URL de descubrimiento devuelva HTTP 200. Para ensayar MFA con Máxima Autoridad, define `SISPLAN_F0_TOTP_SEED` con `openssl rand -hex 32` antes de la **primera** alta de sus credenciales. Conserva esa semilla para los recorridos en ese entorno. Repetir el alta conserva las credenciales y el segundo factor existentes.

```sh
curl --fail --silent --output /dev/null --retry 40 --retry-all-errors --retry-delay 2 \
  http://127.0.0.1:8080/realms/sisplan/.well-known/openid-configuration
docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U sisplan -d sisplan < infra/postgres/fixtures-f0.sql
python3 infra/keycloak/f0.py usuarios
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile
```

La API escucha en `http://127.0.0.1:5080` y Angular en `http://127.0.0.1:4200`. Entra con `f0-analista` para Instituto Horizonte, `f0-analista-b` para Instituto Bahía o `f0-multi` para cambiar de institución. La contraseña inicial es el valor de `SISPLAN_F0_PASSWORD`. La API resuelve por emisor y sujeto verificados las membresías cargadas en PostgreSQL.

### Prueba con navegador y Keycloak

Con la API en marcha y las mismas variables, instala Playwright en un entorno virtual de Python. El comando realiza accesos en Chromium mediante código de autorización y PKCE, verifica el cierre de sesión y prepara un token con vigencia vencida. Si se configuró la semilla, completa el reto TOTP interactivo de Máxima Autoridad. El archivo de salida contiene credenciales de corta duración.

```sh
python3 -m venv /tmp/sisplan-f0-python
/tmp/sisplan-f0-python/bin/pip install playwright
/tmp/sisplan-f0-python/bin/python -m playwright install --with-deps chromium
f0_temp="$(mktemp -d)"
/tmp/sisplan-f0-python/bin/python infra/keycloak/f0.py tokens --salida "$f0_temp/tokens.json"
python3 tests/f0_smoke.py --tokens "$f0_temp/tokens.json"
rm "$f0_temp/tokens.json"
rmdir "$f0_temp"
```

## Rutas

| Método y ruta | Respuesta |
|---|---|
| `GET /api/v1/peis` | Array ordenado por creación descendente |
| `GET /api/v1/peis/{id}` | PEI con `ETag` |
| `POST /api/v1/peis` | Borrador creado, `201`, `Location` y `ETag` |
| `PATCH /api/v1/pei-versiones/{versionId}` | Borrador actualizado y nuevo `ETag` |
| `GET /api/v1/poas` | POA registrados y su presupuesto de referencia, si existe |
| `POST /api/v1/poas` | POA en borrador, `201` |

Las escrituras del PEI reciben `nombre`, `tipo`, `fechaInicio`, `fechaFin`, `mision`, `vision` y `valores`. El nombre admite de 3 a 200 caracteres y cada texto de 1 a 4000. Las fechas usan `AAAA-MM-DD`. La fecha de fin debe ser igual o posterior a la inicial. Para este incremento se acordaron cuatro años calendario inclusivos en el PEI ordinario y cinco en el extraordinario.

`PATCH` envía el `If-Match` exacto obtenido en la consulta. Si falta devuelve `428`. Si otra edición actualizó el borrador devuelve `412`. Los campos inválidos devuelven `422` con un objeto `errors` indexado por campo. El estado del PEI puede ser `borrador`, `en_revision`, `observado`, `corregido`, `aprobado` o `publicado`.

`POST /api/v1/poas` requiere `anio`. El vínculo `peiId` y el monto `presupuestoReferencia` son opcionales. El monto se envía como texto decimal en DOP, por ejemplo `"1500.25"`, y se devuelve con dos decimales o como `null` si se dejó vacío.

Se usa Npgsql con SQL parametrizado. [Npgsql 10.0.3](https://www.nuget.org/packages/Npgsql/10.0.3).
