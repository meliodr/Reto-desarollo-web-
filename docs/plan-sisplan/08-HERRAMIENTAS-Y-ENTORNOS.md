# Herramientas y entornos de SISPLAN

**Base de trabajo v1.0 · 12 de noviembre de 2026.** Una aplicación web modular en un repositorio. Angular consume una API ASP.NET Core y la API concentra las reglas, los permisos, las consultas y la generación de reportes.

| Área | Decisión para el reto | Comprobación antes de ampliar módulos |
|---|---|---|
| Frontend | Angular 22.2.6 con Node 24.22.1, npm 11.9 y TypeScript 6.0. Componentes standalone, rutas diferidas, formularios reactivos tipados y `HttpClient`. CSS de la base y TailwindCSS 4 para utilidades de pantallas nuevas | `npm ci`, build y Vitest pasan desde una copia limpia. Un formulario respeta etiquetas, foco, errores, teclado y pantallas desde 320 px |
| Backend | ASP.NET Core Web API sobre .NET 10 LTS, SDK 10.0.404 para la base. Monolito modular con servicios por dominio y SQL en repositorios | `dotnet build` y el recorrido de borradores pasan con PostgreSQL real |
| Datos | PostgreSQL 18.6, Npgsql 10.0.3, SQL parametrizado y migraciones numeradas aplicadas de forma explícita. Reutilizar el mecanismo del PR #1. Transacciones para publicaciones, cambios de estado y auditoría | Migración desde cero y desde la primera versión, repetición sin duplicados y restauración de una copia |
| Acceso | Keycloak 26.8.2, `keycloak-js` 26.2.5 y ASP.NET Core `JwtBearer`. OAuth2/OIDC por código de autorización con PKCE S256. Contraseña y TOTP obligatorios para el grupo de demo | Iniciar sesión con MFA, consultar `/me`, rechazar una cuenta sin rol y comprobar que un responsable no puede operar otra dependencia |
| API | OpenAPI 3.1 en `contracts/` y `/api/v1`. JSON, fechas ISO, decimales transmitidos como cadenas, `ProblemDetails`, `ETag` e `If-Match` | Cada operación tiene ejemplo de éxito, permiso, errores y prueba de integración. `GET /me` devuelve identidad y alcance de negocio |
| Archivos | Volumen privado de la API. PostgreSQL guarda versiones, metadatos, vínculo y huella SHA-256 | Subir un archivo válido, recuperar su versión y comprobar permisos de descarga y persistencia después de reinicio |
| Exportación XLSX | ClosedXML 0.105.1 en .NET. Exportar `.xlsx` con columnas tipadas y fechas, cifras y texto separados | Abrir en Excel o LibreOffice y conciliar filas y totales con la vista publicada. Texto aportado por usuarios se escribe como texto, sin fórmulas |
| Exportación PDF | Microsoft.Playwright 1.64.1 para .NET con su versión de Chromium y `PdfAsync`. Una plantilla HTML de impresión por informe sobre un modelo de datos común con XLSX | Chromium y sus dependencias están presentes en el entorno de demo. El PDF abre, pagina y mantiene totales y encabezados. Las plantillas escapan el texto y utilizan fuentes y recursos locales. Noto Sans se incluye para PDF y cálculo del ancho de columnas XLSX |
| Calidad | Vitest para Angular, xUnit para reglas de dominio, prueba HTTP contra PostgreSQL y Playwright Test con Chromium para el recorrido completo | Verificación de cálculo, permisos, concurrencia, descargas y al menos un recorrido de usuario con MFA real |
| Colaboración | VS Code, Git y GitHub con rama por tarea, PR y un revisor distinto. GitHub Actions en Ubuntu ejecuta builds, pruebas y migraciones. Portal del equipo para responsables, decisiones y evidencias | Cada tarea queda vinculada al requisito, al PR y al resultado. Los resultados de CI se conservan por commit |
| Entorno | Docker Compose levanta PostgreSQL y Keycloak en loopback. Angular y .NET se ejecutan en el equipo de desarrollo. La demo sirve el build Angular desde ASP.NET Core con un origen para UI y API | Arranque en limpio en otro equipo, con datos sintéticos, volúmenes persistentes y pasos PowerShell/Bash reproducibles |

Las versiones ya probadas pertenecen al prototipo local. La integración de Keycloak, reportes, herramientas de CI y contenedores se valida en el primer hito de cada frente. El equipo fija las versiones resueltas de las nuevas dependencias en su primer PR de integración y conserva `package-lock.json`, las referencias NuGet y tags explícitos. Los parches de seguridad se incorporan con revisión y pruebas.

## Organización de la aplicación

- `frontend/` mantiene áreas de acceso, estrategia, operación, seguimiento, resultados, administración, proyectos y riesgos. Los componentes comunes contienen formularios, tablas, estado y navegación. El diseño es institucional, responsivo y con objetivo WCAG 2.2 AA. Semáforos con texto y color, foco visible y errores junto al campo.
- `backend/` agrupa los mismos dominios. Los endpoints reciben DTO y pasan a servicios. Los servicios controlan transiciones, cálculo y autorización. Los repositorios usan Npgsql y devuelven datos por alcance. `backend/Migrations/` conserva cada cambio SQL.
- `contracts/` conserva OpenAPI, ejemplos y reglas de estados. `tests/` conserva integración y recorridos. `docs/` documenta decisiones, flujos y evidencia. Los scripts de entorno y datos de demostración quedan versionados con la aplicación.
- Dashboards y exportaciones parten del mismo modelo de lectura por corte publicado. Las seis vistas PDF/XLSX comparten las reglas, filtros y fuente de datos. Un proceso alojado en ASP.NET Core genera las alertas internas sin duplicarlas.

## Identidad, acceso y archivos

Keycloak autentica y aplica TOTP. Angular utiliza un cliente público con redirecciones locales exactas y mantiene los tokens en memoria. El interceptor adjunta el token solo a la API de SISPLAN. La API valida firma, emisor, audiencia y caducidad. Usa emisor y sujeto para localizar al usuario y sus asignaciones activas en PostgreSQL. Los guardias de ruta orientan la navegación. La autorización final se comprueba en servidor. Los usuarios de prueba se crean con datos ficticios y las claves y semillas TOTP se generan localmente. Cierre de sesión sale también del proveedor.

Los dos servicios de datos usan bases y credenciales separadas aunque compartan la instancia PostgreSQL. Los secretos quedan en variables locales y configuración de CI. El entorno de integración usa una base desechable. Las URLs locales y el modo de desarrollo del proveedor se reservan a loopback. Antes de considerar publicación institucional se exigirán HTTPS, identidad administrada, copias verificadas y revisión de acceso. La entrega académica prevista es una **demo local reproducible con datos ficticios**.

Cada evidencia admite PDF, DOCX, XLSX, PNG, JPEG o enlace HTTPS. El tamaño máximo inicial es 10 MB por archivo. El sistema valida formato y tamaño, asigna un nombre interno, registra la huella y guarda fuera de la carpeta pública. Las descargas pasan por autorización y se entregan como adjuntos. Los enlaces se muestran como referencias y el servidor no los descarga. Una sustitución crea otra versión. La copia de seguridad de una demo incluye PostgreSQL y su volumen de evidencias del mismo corte.

## Fuentes técnicas

- [Versiones compatibles de Angular](https://angular.dev/reference/versions) y [guía de Tailwind con Angular](https://angular.dev/guide/tailwind).
- [Soporte de .NET](https://dotnet.microsoft.com/es-es/platform/support/policy) y [validación JWT en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
- [Adaptador JavaScript de Keycloak](https://www.keycloak.org/securing-apps/javascript-adapter), [contenedores](https://www.keycloak.org/server/containers), [administración y OTP](https://www.keycloak.org/docs/latest/server_admin/) y [bases compatibles](https://www.keycloak.org/server/db).
- [ClosedXML y licencia](https://github.com/ClosedXML/ClosedXML), [Playwright .NET PDF](https://playwright.dev/dotnet/docs/api/class-page#page-pdf) y [Playwright en CI](https://playwright.dev/docs/ci-intro).
