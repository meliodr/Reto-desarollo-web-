# API de planificación estratégica

La API .NET 10 usa PostgreSQL para borradores, ejes, objetivos, indicadores, publicación y generación de POA. [Configurar Keycloak para aprobar y publicar](../docs/plan-sisplan/06-PRIMER-INCREMENTO.md#habilitar-el-acceso-institucional-local).

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

`POST` y `PATCH` reciben `nombre`, `tipo`, `fechaInicio`, `fechaFin`, `mision`, `vision` y `valores`. El nombre admite de 3 a 200 caracteres y cada texto de 1 a 4000. Las fechas usan `AAAA-MM-DD`. La fecha de fin debe ser igual o posterior a la inicial. Para este incremento se acordaron cuatro años calendario inclusivos en el PEI ordinario y cinco en el extraordinario.

`PATCH` envía el `If-Match` exacto obtenido en la consulta. Si falta devuelve `428`. Si otra edición actualizó el borrador devuelve `412`. Los campos inválidos devuelven `422` con un objeto `errors` indexado por campo. El estado del PEI puede ser `borrador`, `en_revision`, `aprobado` o `publicado`.

Se usa Npgsql con SQL parametrizado. [Npgsql 10.0.3](https://www.nuget.org/packages/Npgsql/10.0.3).
