# F0.1 · Contrato institucional para PEI y POA

**Corte del 26 de noviembre de 2026.** F1 y F2 comparten una sesión, una institución activa y sus catálogos. La definición de los DTO y de las rutas actuales está en [OpenAPI](../../contracts/pei.openapi.yaml). Los equipos pueden simular respuestas y preparar solicitudes con los [ejemplos JSON](../../contracts/fixtures/f0.json).

## Alcance y fuentes

| Fuente | Decisión para la entrega |
|---|---|
| [SRS v2 del maestro](https://campusvirtual.intec.edu.do/pluginfile.php/1432013/mod_assign/introattachment/0/SRS%20v2%20%20Reto%20PEI%20y%20POA%20Noviembre%20Enero%202027_Intec.pdf?forcedownload=1), pp. 2–3, 7–10 | Se conservan los ocho actores y la RACI. PEI y POA pueden operar de forma independiente. En modo integrado, se utiliza la estructura estratégica cargada como referencia para el POA. |
| Referencias funcionales de F0 | Los criterios de aceptación son los del [plan por fases](11-PLAN-POR-FASES-SRS-V2.md#pruebas-de-f0), derivados del SRS v2 y de las reglas técnicas de SISPLAN. |
| Ejes y Objetivos DIGEMAPS y POA TI 2026 V2 adjuntos a la actividad | El PEI usa el catálogo estratégico provisto. F2 concilia la programación POA con el segundo Excel. Los ejemplos de F0 utilizan identidades ficticias. |
| SRS v2 §4 y §8.1 y [adaptación al SRS](10-ADAPTACION-SRS-V2.md) | El Director de Planificación ejecuta la publicación oficial. La Máxima Autoridad la autoriza para ambos planes. El Comité Estratégico interviene en la cadena de aprobación del PEI como actor técnico adicional. El flujo de aprobación del POA se implementa en F2. |

## Selección de institución y modos

1. Angular obtiene un token de acceso mediante OIDC. La API verifica el token y usa el par de claims `iss` y `sub` para consultar las membresías vigentes.
2. `GET /api/v1/sesion` devuelve el actor, sus instituciones disponibles y `institucionActivaId`. Se omiten membresías, instituciones y dependencias de asignación inactivas. Si la solicitud no selecciona institución, el servidor usa la primera disponible por UUID.
3. Para elegir otra institución se envía un único encabezado `X-Institucion-Id` con su UUID. La institución activa, rol, dependencia y módulos se resuelven de nuevo en el servidor. Tener un mismo sujeto en dos instituciones admite roles distintos.
4. `GET /api/v1/organizacion` entrega dependencias, períodos y unidades de medida de la institución. Los dos perfiles de área ven su propia dependencia. Una dependencia inactiva sigue siendo visible en el catálogo institucional para las cuentas con lectura global.
5. Las rutas PEI requieren el módulo PEI y las rutas POA requieren POA. La consulta de POA vinculados desde un PEI exige ambos. Con solo POA se puede crear un plan autónomo omitiendo `peiId`; una referencia explícita a PEI se deniega. Las propiedades `peiId` y `peiNombre` se devuelven nulas al listar con PEI desactivado.

Un plan creado por un Responsable de Área lleva su dependencia. Si el responsable la indica expresamente, debe coincidir con la suya. Las cuentas institucionales con permiso para formular POA pueden seleccionar una dependencia activa de su institución. Una misma institución admite un único POA independiente por año.

## Matriz de autorización del incremento

Leer planes exige la membresía verificada y el módulo habilitado. El acceso al PEI es institucional. La lectura del POA por un rol de área se limita a su dependencia. La columna PEI incluye la creación del borrador, edición de su estructura y carga del catálogo. La última columna recoge las acciones de transición que puede presentar el servidor según estado e historial.

| Actor del SRS / rol técnico | Leer planes | Escribir estructura PEI | Crear POA | Consultar auditoría | Flujo PEI |
|---|---|---|---|---|---|
| Super Administrador · `SuperAdministrador` | Sí | — | — | — | — |
| Administrador Institucional · `AdministradorInstitucional` | Sí | — | — | Sí | — |
| Director de Planificación · `DirectorPlanificacion` | Sí | — | Sí | — | Revisar, devolver, publicar |
| Analista de Planificación · `AnalistaPlanificacion` | Sí | Sí | Sí | — | Enviar, corregir |
| Responsable de Área · `ResponsableArea` | Sí, con alcance de área en POA | — | Sí, en su área | — | — |
| Director de Área · `DirectorArea` | Sí, con alcance de área en POA | — | — | — | — |
| Auditor · `Auditor` | Sí | — | — | Sí | — |
| Máxima Autoridad · `MaximaAutoridad` | Sí | — | — | — | Aprobar o devolver |
| Comité Estratégico · `ComiteEstrategico` (§8.1) | Sí | — | — | — | Validar o devolver |

«—» indica que el rol carece de ese permiso en las rutas del incremento. El código histórico `Planificador` conserva los permisos de `AnalistaPlanificacion`. La configuración de instituciones y catálogos se realiza por aprovisionamiento. Las responsabilidades RACI de administración y aprobación de área permanecen como referencia para sus funciones posteriores.

## Estados, errores y auditoría

- **PEI.** Inicia en `borrador`. Enviar pasa a `en_revision`. Revisión y validación de Comité quedan en el historial sin cambiar el estado. Devolver requiere un motivo y pasa a `observado`. Corregir pasa a `corregido` y permite un nuevo envío. Aprobar pasa a `aprobado` y publicar a `publicado`.
- **POA.** Crear devuelve `borrador`. El DTO preserva los estados históricos de formulación, revisión, observación, aprobación, rechazo, ejecución y cierre. Publicar un PEI permite vincular un POA mediante una creación explícita.
- **Auditoría.** `GET /api/v1/auditoria` entrega hasta 100 escrituras en orden descendente de ID. Para continuar se pasa el último ID en `?antesDe=`. Cada evento identifica institución, emisor, sujeto, fecha, tabla, referencia del registro y operación SQL. Las consultas se limitan a la institución activa. Las escrituras confirmadas y su registro se guardan en la misma transacción. La tabla de auditoría conserva los valores anterior y nuevo. Su consulta HTTP ofrece los metadatos del evento. El historial de transiciones PEI conserva sus datos específicos.

| HTTP | Causa esperada | Comportamiento del consumidor |
|---|---|---|
| 400 | JSON, selección institucional o cursor inválido | Corregir el dato indicado. |
| 401 | Token ausente, inválido o vencido | Recuperar la sesión. |
| 403 | Sin membresía efectiva, módulo apagado, acción sin permiso o dependencia inválida | Releer el contexto y mostrar la denegación. |
| 404 | Recurso por ID inexistente o fuera de ámbito | Mostrar elemento no disponible. |
| 409 | POA duplicado, vínculo PEI indisponible, solapamiento o estado incompatible | Conservar los datos e indicar el conflicto que reporta la operación. |
| 412 / 428 | `If-Match` obsoleto / ausente en una ruta versionada | Releer la versión y conservar la entrada antes de reintentar. |
| 415 / 422 | Tipo de contenido no admitido / campos inválidos | Usar JSON y asociar los mensajes de `errors` a sus campos. |
| 503 | PostgreSQL temporalmente indisponible | Señalar que no se pudo completar la solicitud. |

El cliente usa el código HTTP aunque el servidor no envíe cuerpo, como puede ocurrir en un desafío de autenticación. En errores de aplicación, puede presentar `title` y `detail` si están disponibles. Los datos de una institución ajena quedan fuera de la respuesta.

## Evidencia compartida para F3.2 y F4.2

El [JSON Schema](../../contracts/evidencia.schema.json) fija el DTO de lectura. F0 entrega el contrato y un ejemplo de sustitución de acta; F3 y F4 construyen las operaciones para registrar, leer, abrir, revisar y reemplazar. El ejemplo de URL usa un dominio reservado para documentación.

| Regla del servicio previsto | Criterio de aceptación |
|---|---|
| Identidad y ámbito | El servidor fija institución, autor y fecha. La evidencia pertenece a una medición PEI o a un avance POA y a su período. La consulta, el historial y la apertura del soporte requieren acceso al mismo registro, módulo, institución y dependencia. |
| Fuente y tipo | Una versión contiene un archivo o una URL HTTPS. Acta y contrato identifican el uso del soporte. El formato físico admite PDF, Excel XLS/XLSX, JPEG o PNG. La clasificación no depende del nombre del archivo. |
| Límites de implementación propuestos | Hasta 10 MiB por archivo, 255 caracteres para nombre, 2.048 para una URL y 1.000 para descripción. Son decisiones locales del contrato. La carga valida el contenido y el formato declarado. |
| Versiones e historial | Se conserva un ID estable por evidencia. Reemplazar agrega una versión consecutiva y preserva contenido, autor, fecha y estado de las anteriores. La última versión es la vigente. El alta y cada reemplazo inician en `pendiente`. La revisión admite `validada` u `observada`. |
| Reintentos y concurrencia | La escritura incluye una clave de idempotencia por actor e institución. Un reintento con la misma clave y contenido devuelve el resultado previo. Reutilizarla con otro contenido devuelve 409. Reemplazar usa la versión vigente precondicionada. Una carga fallida conserva la versión anterior. |
| Apertura | F3/F4 comprueban permisos antes de devolver el archivo o la URL. Los controles de SISPLAN protegen la entrada y el historial. El proveedor externo controla quién puede abrir su URL una vez conocida. |

## Decisiones operativas para el cierre de F0

| Tema | Regla que aplican F1 y F2 | Fundamento |
|---|---|---|
| Segundo factor | Todas las cuentas ficticias del grupo de demostración ingresan por Keycloak con contraseña y un código TOTP propio. La cuenta de prueba sin membresía también pasa por el proveedor y SISPLAN deniega su acceso. Las semillas y los códigos quedan fuera de las capturas y del repositorio. | Política de autenticación del [entorno de demostración](08-HERRAMIENTAS-Y-ENTORNOS.md). |
| Comité Estratégico | Una persona con membresía vigente de Comité representa su validación. Puede validar o devolver el PEI tras la revisión del Director. Quien envió o revisó en ese ciclo carece de permiso para validarlo como Comité. El historial identifica a la persona que tomó la decisión. | Flujo PEI del SRS v2, pág. 7, y regla de separación de funciones del primer incremento. Se adopta una validación representativa por ciclo. |
| Aprobación y publicación | Máxima Autoridad aprueba la versión oficial. El Director de Planificación ejecuta su publicación. F0 fija ambas responsabilidades para PEI y POA. La implementación del flujo POA corresponde a F2. | RACI del SRS v2, págs. 2–3. |

Una revisión posterior del quórum del Comité o del alcance de MFA se tratará como un cambio de política, con actualización de la matriz y sus pruebas. La planificación de horas y revisores pertenece al seguimiento del equipo.

La semilla de [organización y membresías](../../infra/postgres/fixtures-f0.sql) alimenta las verificaciones con PostgreSQL. Los [casos HTTP de F0](../../tests/f0_smoke.py) ejercitan la matriz y los tres modos. Para simular las respuestas en Angular o API se puede cargar el JSON de ejemplos; el valor de una fecha, un nuevo ID de plan o un evento de ejemplo no representa una escritura ejecutada.

Desde la raíz, ejecutar `python3 contracts/verificar_f0.py` con PyYAML, jsonschema y `openapi-spec-validator==0.7.2`. El verificador aplica el esquema completo OpenAPI 3.1, comprueba referencias y formatos, valida los ejemplos y ejercita los límites del contrato de evidencias. En un entorno sin ese paquete se puede pasar `--esquema-oas RUTA` con una copia local del [esquema oficial de la misma versión](https://spec.openapis.org/oas/3.1/schema/2022-10-07).
