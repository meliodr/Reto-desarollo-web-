# API de planificación estratégica

La API .NET 10 usa PostgreSQL para formular, revisar y publicar PEI y para crear o consultar POA independientes o vinculados. [Matriz de permisos del SRS v2](../docs/plan-sisplan/14-CONTRATO-F0.md#matriz-de-autorización-del-incremento).

## Arranque local de F0

Desde la raíz del repositorio, prepara cinco claves locales distintas. Las siguientes variables duran la sesión de shell. Para reutilizar los volúmenes en otra sesión, conserva sus valores fuera del repositorio.

```sh
export SISPLAN_DB_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_APP_DB_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_KEYCLOAK_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_F0_PASSWORD="$(openssl rand -hex 24)"
export SISPLAN_F0_TOTP_SEED="$(openssl rand -hex 32)"
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

El cliente local `sisplan-web` exige contraseña y TOTP al iniciar una sesión nueva. Las 15 identidades ficticias tienen un autenticador individual. Conserva la raíz `SISPLAN_F0_TOTP_SEED` del entorno para los arranques siguientes.

**Actualización de un volumen anterior.** Recupera las contraseñas originales. Si Máxima Autoridad ya tenía TOTP en la versión anterior, guarda el valor usado entonces en `SISPLAN_F0_TOTP_AUTORIDAD_ANTERIOR` y crea una raíz independiente para `SISPLAN_F0_TOTP_SEED` una sola vez. El alta agrega el factor que falte y conserva los dispositivos y las contraseñas existentes. Las credenciales privadas se guardan fuera del repositorio.

```sh
curl --fail --silent --output /dev/null --retry 40 --retry-all-errors --retry-delay 2 \
  http://127.0.0.1:8080/realms/sisplan/.well-known/openid-configuration
docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U sisplan -d sisplan < infra/postgres/fixtures-f0.sql
python3 infra/keycloak/f0.py usuarios
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile
```

La API escucha en `http://127.0.0.1:5080` y Angular en `http://127.0.0.1:4200`. Entra con `f0-analista` para Instituto Horizonte, `f0-analista-b` para Instituto Bahía o `f0-multi` para cambiar de institución. La contraseña inicial es el valor de `SISPLAN_F0_PASSWORD`. La API resuelve por emisor y sujeto verificados las membresías cargadas en PostgreSQL.

Cuando Angular te envíe a Keycloak, introduce el usuario y la contraseña. Para obtener el código de seis dígitos de `f0-multi`, ejecuta `python3 infra/keycloak/f0.py codigo multi` en tu terminal local con las variables del entorno. Para usar una aplicación autenticadora, ejecuta `python3 infra/keycloak/f0.py codigo multi --registrar` en privado y registra la clave personal mostrada como TOTP basado en tiempo, SHA-1, seis dígitos y periodo de 30 segundos. Sustituye `multi` por el alias de la cuenta ficticia que estés probando. Si una cuenta existente usa un dispositivo propio, introduce el código de ese dispositivo.

### Prueba con navegador y Keycloak

Con la API en marcha, inicia Angular en el puerto 4200 como indica la [guía del frontend](../frontend/README.md#ejecución-local). El verificador recibe el retorno OIDC en una página HTML estática del frontend. En otra terminal con las mismas variables locales, instala Playwright en un entorno virtual de Python. Chromium recorre los formularios reales de contraseña y TOTP de las 15 identidades y verifica su sujeto mediante el flujo de código de autorización con PKCE. El ensayo comprueba el rechazo de un código incorrecto, el cierre de sesión y la emisión de un token que se deja caducar. El archivo de salida contiene credenciales de corta duración.

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
