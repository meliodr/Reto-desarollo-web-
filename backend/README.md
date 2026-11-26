# API de planificación estratégica

La API .NET 10 usa PostgreSQL para formular, revisar y publicar PEI y para crear o consultar POA independientes o vinculados. [Configurar Keycloak para el flujo del SRS v2](../docs/plan-sisplan/10-ADAPTACION-SRS-V2.md#verificación-del-incremento).

## Arranque

Configura estas variables en la sesión local. Sustituye los datos de conexión con los de tu base.

```sh
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Sisplan='Host=127.0.0.1;Port=5432;Database=sisplan;Username=USUARIO;Password=CONTRASEÑA'
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile -- --migrate
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile
```

La API escucha en `http://127.0.0.1:5080`. Para cambiar el puerto usa `Sisplan__Puerto`. Angular debe dirigir `/api/v1` a esa dirección mediante su proxy local. La API requiere `Development` y conexiones por loopback. Conserva la cadena de conexión fuera del repositorio.

Cada migración aplicada se registra con su huella. Repetir `--migrate` conserva los datos y reconoce las versiones aplicadas. Ejecuta la migración antes de iniciar la API.

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
