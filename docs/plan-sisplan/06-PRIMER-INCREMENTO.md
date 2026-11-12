# Primer incremento de SISPLAN

El recorrido permite listar, crear, abrir y editar borradores de PEI en Angular. La API guarda los datos en PostgreSQL. La pantalla muestra el período derivado de las fechas y el servidor asigna el estado borrador. Corresponde a la cabecera de RF-001. El siguiente tramo incorpora ejes, objetivos e indicadores y define las transiciones de revisión y publicación.

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

La API de esta fase se ejecuta en `http://127.0.0.1:5080` y requiere el entorno Development. Usar una base y datos de prueba locales. El acceso institucional con OIDC, MFA y autorización por dependencia se incorpora en otro incremento.

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
- La revisión, aprobación y publicación requieren la matriz de permisos del equipo. La generación de POA se activa en la transición que se acuerde.

## Resultado de la revisión local

El 12 de noviembre se comprobó la compilación de Angular y la API .NET 10. La prueba HTTP recorrió el proxy de Angular contra PostgreSQL 18 y recuperó el borrador editado tras reiniciar la API. También comprobó los períodos válidos, `422` para fechas inválidas, `428` sin etiqueta y `412` al usar una revisión anterior. Dos solicitudes concurrentes con la misma etiqueta devolvieron una actualización exitosa y un conflicto. La base de verificación fue desechable.

Angular aprobó 11 pruebas automatizadas, incluidas validación de períodos, envío de `If-Match` y conservación de los campos editados ante `412`.
