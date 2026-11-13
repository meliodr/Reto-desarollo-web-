# Primer incremento de SISPLAN

El recorrido permite listar, crear, abrir y editar borradores de PEI en Angular. La API guarda los datos en PostgreSQL. La pantalla muestra el período derivado de las fechas y el servidor asigna el estado borrador. Corresponde a la cabecera de RF-001. En un PEI guardado se pueden gestionar ejes y registrar objetivos. Los objetivos admiten indicadores y mediciones. En el entorno con acceso institucional, otro planificador puede aprobar el PEI antes de publicarlo y generar sus POA.

## Arranque local

Requiere Node 24 desde 24.15.0, npm, el SDK de .NET 10 y PostgreSQL 18. Se puede usar la base local de Docker Compose o una instancia PostgreSQL de desarrollo ya instalada. Los ejemplos se ejecutan desde la raíz del repositorio.

### 1. Base de datos

Crear un archivo `.env` local con una clave de desarrollo y levantar el servicio. Docker Compose conserva los datos en el volumen `sisplan-postgres`. Si ya se tiene PostgreSQL, crear una base de desarrollo y ajustar el host, puerto y credenciales de la conexión.

```dotenv
SISPLAN_DB_PASSWORD=elige_una_clave_local
```

```sh
docker compose up -d --wait postgres
```

### 2. API

Configurar la conexión con la misma clave del paso anterior. En PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Sisplan = 'Host=127.0.0.1;Port=55432;Database=sisplan;Username=sisplan;Password=elige_una_clave_local'
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile -- --migrate
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile
```

En Bash:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Sisplan='Host=127.0.0.1;Port=55432;Database=sisplan;Username=sisplan;Password=elige_una_clave_local'
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile -- --migrate
dotnet run --project backend/Sisplan.Api.csproj --no-launch-profile
```

La API de esta fase se ejecuta en `http://127.0.0.1:5080` y requiere el entorno Development. Usar una base y datos de prueba locales. Para habilitar el flujo autenticado de publicación, sigue la sección de acceso institucional local.

### 3. Angular

En otra terminal:

```sh
cd frontend
npm ci
npm start
```

Abrir `http://localhost:4200`. La configuración local del frontend dirige `/api` a la API en el puerto 5080. El [contrato OpenAPI](../../contracts/pei.openapi.yaml) fija las rutas y ejemplos compartidos.

## Verificación

1. Crear un PEI ordinario con inicio en 2026 y fin en 2029. Guardar nombre, misión, visión y valores.
2. Volver al listado y abrir el borrador. Comprobar que conserva los datos y el período.
3. Editar la visión y verificar que persiste al recargar.
4. Abrir el borrador en dos pestañas. Guardar un cambio en una y luego intentar guardar desde la otra. La segunda debe informar el conflicto y conservar lo escrito para revisión.
5. Probar un ordinario con un período de dos años. La validación debe identificar las fechas.

```sh
python3 tests/pei_smoke.py
```

La prueba registra un borrador identificable en la base de desarrollo. Cubre creación, consulta, edición, listado, período y conflicto de versión. Con el ID que imprime, se puede reiniciar la API y ejecutar `python3 tests/pei_smoke.py --verify-id ID` para comprobar que el cambio sigue guardado.

## Decisiones para revisar

- Se cuentan años calendario inclusivos para distinguir el ordinario de cuatro años y el extraordinario de cinco. Los días concretos se conservan y se valida que el fin no preceda al inicio.
- La creación genera un identificador del PEI y otro de su borrador. La revisión usada para concurrencia se comunica con `ETag` y `If-Match`.
- La publicación genera los POA anuales y sus cuatrimestres. La aprobación exige un planificador distinto de quien envió el borrador.

## Resultado de la revisión local

El 12 de noviembre se comprobó la compilación de Angular y la API .NET 10. La prueba HTTP recorrió el proxy de Angular contra PostgreSQL 18 y recuperó el borrador editado tras reiniciar la API. También comprobó los períodos válidos, `422` para fechas inválidas, `428` sin etiqueta y `412` al usar una revisión anterior. Dos solicitudes concurrentes con la misma etiqueta devolvieron una actualización exitosa y un conflicto. La base de verificación fue desechable.

Angular aprobó 11 pruebas automatizadas, incluidas validación de períodos, envío de `If-Match` y conservación de los campos editados ante `412`.

## Ejes y objetivos del PEI

En un borrador guardado, abre **Ejes estratégicos** y crea un eje. Al editarlo se agrega una revisión. La inactivación agrega otra revisión y conserva las anteriores. Desde **Ver objetivos** se puede guardar un objetivo con código, nombre, descripción, resultado esperado y línea de acción. El código es único dentro de su eje. Los objetivos existentes siguen disponibles para consulta si el eje se inactiva.

Las migraciones `0002` y `0003` se aplican con el mismo comando `--migrate`. Para verificar el incremento con PostgreSQL local:

```sh
python3 tests/pei_smoke.py
python3 tests/ejes_smoke.py
python3 tests/objetivos_smoke.py
```

La prueba de ejes imprime dos identificadores separados por coma. Reinicia la API y vuelve a ejecutar la prueba con `--verify-ids ID_PEI,ID_EJE` para verificar que las revisiones siguen guardadas. Los endpoints de edición reciben la revisión y su `ETag` en `If-Match`; un conflicto devuelve `412`.

El 13 de noviembre se comprobó el recorrido en Chromium con Angular, .NET y PostgreSQL 18: creación y edición del borrador, alta y modificación del eje, consulta de tres revisiones y registro del objetivo con los cinco campos. Se comprobó que el objetivo seguía visible tras recargar e inactivar el eje. Las pruebas HTTP cubrieron edición concurrente, rutas cruzadas entre PEI, duplicados y campos inválidos. También se probó actualizar una base con borradores previos y aplicar las migraciones dos veces. El formulario se comprobó a 1280, 390 y 320 píxeles para detectar desbordamiento horizontal.

## Indicadores, publicación del PEI y POA anuales

Desde un objetivo se puede registrar un indicador con nombre, fórmula, unidad, línea base, meta y frecuencia. La fórmula se guarda como definición y las mediciones se registran por fecha. Durante la formulación, la API permite una medición por fecha e indicador, dentro de las fechas del PEI. La consulta sigue disponible al enviar el PEI a revisión. La captura de mediciones sobre planes publicados queda para el módulo de seguimiento.

### Habilitar el acceso institucional local

1. Define una contraseña local en `SISPLAN_KEYCLOAK_PASSWORD` y arranca `docker compose --profile institucional up -d postgres keycloak`. Keycloak importa el realm `sisplan` desde `infra/keycloak/sisplan-realm.json`.
2. En la administración de Keycloak, crea dos usuarios con credenciales distintas y asigna a cada uno el rol de cliente `sisplan-web → Planificador`. Para probar acceso denegado, crea una tercera cuenta sin ese rol.
3. Arranca la API con `Sisplan__Oidc__Url=http://127.0.0.1:8080` además de la configuración de PostgreSQL. Aplica las migraciones `0004` y `0005` con el comando habitual. Abre Angular desde `http://127.0.0.1:4200`.
4. Entra con el primer planificador, crea un PEI y agrega un eje activo con un objetivo y su indicador. Envía el PEI a revisión. Abre la sesión del segundo planificador en otro perfil de navegador, aprueba y publica. La publicación muestra un POA en formulación por año del PEI y los tres cortes oficiales por POA.

Sin URL OIDC, el entorno local permite trabajar en borradores en loopback. El flujo de aprobación, publicación y consulta de POA requiere sesión de planificador. La configuración de este incremento cubre el acceso al flujo de planificación. La exigencia de MFA y los demás roles de RNF-001 se verifican en incrementos posteriores.

Para reproducir las pruebas HTTP de indicadores, ejecuta `python3 tests/indicadores_smoke.py` en el entorno local de borradores. Para el flujo autenticado, ejecuta `python3 tests/flujo_smoke.py --year 2026` en una base desechable con `SISPLAN_PLANIFICADOR_1_TOKEN` y `SISPLAN_PLANIFICADOR_2_TOKEN` de dos planificadores. `SISPLAN_OTRO_ROL_TOKEN` permite verificar el rechazo de una cuenta sin ese permiso. Mantén los tokens de prueba fuera del repositorio.

La fecha de vigencia de cada POA se limita a su intersección con el intervalo del PEI. Los POA futuros se pueden consultar en formulación y aparecen sin vigencia para la fecha de consulta. Los cuatrimestres conservan sus meses oficiales y se marcan fuera de vigencia cuando quedan completamente fuera del intervalo. RF-012 se mantiene parcial hasta verificar el registro de avances por corte y la emisión con evidencia.

### Verificación del recorrido de publicación

El 13 de noviembre pasaron 18 pruebas de Angular y la compilación .NET. Con dos cuentas de Keycloak 26.8.2 se creó un PEI ordinario en Chromium, se registraron su eje, objetivo e indicador, y una segunda cuenta aprobó y publicó. Angular mostró cuatro POA y los tres cuatrimestres de cada uno. La prueba HTTP con un PEI extraordinario comprobó cinco POA, el rechazo de autoaprobación y cuentas sin permiso, la vigencia por fecha, el reintento sin duplicados y el bloqueo de publicaciones simultáneas con fechas solapadas. PostgreSQL 18 también conservó los borradores de la versión anterior al aplicar las migraciones.
