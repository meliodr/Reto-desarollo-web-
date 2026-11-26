# Base institucional de SISPLAN · Criterios de cierre de F0

**Semana 4 · Implementación en el PR #2 sobre el primer incremento PEI–POA**

La [implementación F0](14-CONTRATO-F0.md) y el [arranque vigente](../../backend/README.md#arranque-local-de-f0) actualizan los pasos y las decisiones. La línea base de este documento corresponde al incremento anterior.

El primer objetivo de F0 es que PEI y POA puedan trabajar con un contrato común y datos aislados. El recorrido de semana 5 necesita dos instituciones, permisos efectivos y trazabilidad de las escrituras que se van a demostrar. El [plan completo](11-PLAN-POR-FASES-SRS-V2.md#subfases-de-f0) mantiene los criterios de cierre de F0 y el [calendario](12-ENTREGA-SEMANA-7.md) sitúa el candidato al final de la semana 6.

## Cierre por subfase

| Subfase | Qué debe poder reproducir el equipo |
|---|---|
| F0.1 · Contratos | Validar el documento OpenAPI 3.1 completo, la evidencia compartida y los ejemplos. La [matriz de roles y decisiones](14-CONTRATO-F0.md) fija el segundo factor, la representación del Comité, la aprobación y la publicación. |
| F0.2 · Organización | Abrir las dos instituciones con sus dependencias, períodos y catálogos. Probar solo PEI, solo POA y ambos módulos. |
| F0.3 · Permisos | Ingresar con las cuentas ficticias de los ocho actores y del Comité, comprobar el segundo factor de cada cuenta y recorrer la matriz de permisos y denegaciones. |
| F0.4 · Migración y trazabilidad | Aplicar y repetir las migraciones desde cero y sobre datos históricos asignados. Verificar IDs, relaciones, auditoría y aislamiento entre instituciones con el login de la aplicación. |

Para el cierre se usa el resultado de ambos controles sobre el mismo commit del PR. El trabajo F1/F2 puede consumir los contratos y los datos de demostración documentados. [Controles de la revisión actual](https://github.com/meliodr/Reto-desarollo-web-/pull/2/checks)

## Verificación anterior

El [PR #2](https://github.com/meliodr/Reto-desarollo-web-/pull/2) contiene el cambio completo sobre el primer incremento. La [ejecución del 26 de noviembre](https://github.com/meliodr/Reto-desarollo-web-/actions/runs/45904335830) verificó lo siguiente en un entorno desechable.

| Caso | Comprobación ejecutada |
|---|---|
| F0-01 y F0-08 | Quince identidades de Keycloak, acceso con PKCE, token expirado, cierre de sesión y segundo factor TOTP de Máxima Autoridad en Chromium. |
| F0-02 | Ocho actores y Comité. Acciones concedidas y denegadas en las transiciones PEI, escritura POA por área, consulta del auditor y rechazo de una dependencia inactiva. |
| F0-03 a F0-05 | Dos POA del mismo año, duplicado institucional rechazado, relaciones y detalles en su ámbito. Solicitudes A y B alternadas con una sola conexión en el grupo de la API. |
| F0-06 | Instalación limpia y actualización del ejecutable anterior con datos. Segunda ejecución de migraciones, conservación de IDs, vínculos y presupuestos y verificación del historial de escrituras. |
| F0-07 | PEI y POA por separado y juntos. El cambio de institución en Angular actualiza las acciones y el historial visible. |

Angular pasó 26 pruebas y la compilación de producción. Los cinco recorridos HTTP del primer incremento también pasaron. Las dos capturas del recorrido en navegador están en el artefacto **capturas-f0** de la ejecución. Los [controles de la revisión actual](https://github.com/meliodr/Reto-desarollo-web-/pull/2/checks) vuelven a ejecutar la matriz con cada cambio del PR.

## Punto de partida anterior a F0

| Área | Evidencia del 26 de noviembre | Implicación para F0 |
|---|---|---|
| Angular | Compilación correcta y 21 pruebas aprobadas en ocho archivos. | Conservar esta regresión al introducir institución activa y permisos. |
| API y datos | .NET compiló sin errores ni advertencias. PostgreSQL aplicó las siete migraciones y las reconoció en la segunda ejecución. Pasaron los cinco scripts HTTP existentes. Se recuperó un POA anterior a `0007` con sus datos intactos y presupuesto nulo. | Agregar la siguiente migración sobre la base actual y probar tanto instalación limpia como actualización con datos. |
| Identidad | El flujo HTTP autenticado pasó con cinco cuentas de prueba de Keycloak: Analista, Director de Planificación, Comité, Máxima Autoridad y Auditor. | Los permisos actuales usan roles de cliente. Falta resolver la pertenencia institucional y verificar un inicio de sesión real con MFA si se exige. |
| Instituciones | La base actual no identifica la institución de los planes ni aplica políticas de seguridad por fila a sus datos. El índice del POA independiente es único por año a nivel global. | Al incorporar instituciones, conservar la unicidad dentro de cada una. Probar dos POA independientes del mismo año, uno por institución. |
| Entorno | La configuración actual de la API requiere `Development` y conexiones por loopback. | La verificación inicial se hace en local. Registrar como pendiente de entrega la configuración del entorno candidato y sus pruebas de acceso. |

## Secuencia utilizada para la implementación

| Subfase | Primer cambio verificable | Criterio para revisarlo |
|---|---|---|
| **F0.1 · Alcance y contratos** | Documentar identidad, institución activa, pertenencia, módulos habilitados, auditoría y el [contrato común de evidencias](12-ENTREGA-SEMANA-7.md#contrato-común-de-evidencias). Preparar datos de prueba y fijar qué errores verá Angular para cada caso. | PEI y POA pueden empezar con el mismo contrato y datos de prueba. Las decisiones que dependen del aula o del maestro se registran con su estado. |
| **F0.2 · Instituciones y organización** | Registrar dos instituciones ficticias con sus dependencias y períodos. Preparar las configuraciones solo PEI, solo POA y ambos módulos. | Se puede usar cada módulo con la configuración esperada, dentro de la institución activa. |
| **F0.3 · Identidad y permisos** | Relacionar la identidad verificada de Keycloak con las membresías de SISPLAN. Resolver por solicitud la institución activa, el rol y la dependencia aplicable. | Casos positivos y negativos para cada combinación acordada. Las reglas usan la membresía verificada; el servidor verifica la pertenencia antes de aceptar una institución seleccionada por el cliente. |
| **F0.4 · Migración, aislamiento y auditoría** | Asignar institución a los datos históricos con una correspondencia explícita. Aplicar aislamiento en API y PostgreSQL a los planes y sus registros dependientes. Registrar las operaciones auditables. | Pasan la migración con datos y sin datos, la matriz de aislamiento y la consulta del historial. Las pruebas usan una cuenta de aplicación sujeta a las políticas de filas y una cuenta de migración con permisos separados. |

F0.2 y F0.3 se pueden preparar en paralelo tras el contrato de F0.1. F0.4 toma ese contrato para preparar la migración y requiere instituciones y membresías para probar el flujo completo. Las pantallas generales de administración se priorizan según la rúbrica. El primer recorrido incluye aislamiento y permisos desde el inicio.

## Casos iniciales de QA

Usar dos instituciones ficticias, A y B, y al menos dos dependencias en A. Preparar cuentas de Analista y Responsable de Área separadas por institución, un Auditor de A y las cuentas que revisan, validan, aprueban y publican en el recorrido. Ampliar los casos a los ocho actores del SRS y al rol técnico de Comité según la matriz acordada.

| Caso | Resultado esperado |
|---|---|
| **F0-01 · Sesión** | Una solicitud protegida sin sesión válida devuelve `401`. Una cuenta autorizada puede entrar en su institución activa. |
| **F0-02 · Rol y dependencia** | Un Auditor consulta en su ámbito y no puede escribir. Una cuenta de área no modifica datos de otra dependencia cuando la operación exige pertenecer a ella. |
| **F0-03 · Aislamiento** | A y B pueden registrar cada una su POA independiente para el mismo año. Un duplicado dentro de la misma institución sigue devolviendo `409`. |
| **F0-04 · Acceso cruzado** | Listados, detalles por ID, consultas ligadas y cambios devuelven solo resultados autorizados. Un acceso ajeno queda denegado con la respuesta acordada y sin revelar sus datos. |
| **F0-05 · Contexto en base de datos** | Con la cuenta de la API, las consultas respetan la institución activa. Alternar solicitudes A/B sobre el conjunto compartido de conexiones mantiene separados ambos contextos. Una solicitud sin contexto institucional no recupera datos protegidos. |
| **F0-06 · Migración y auditoría** | Una base con datos previos conserva IDs, vínculos PEI–POA y el presupuesto nulo o informado. Aplicar dos veces produce el mismo resultado. Las escrituras auditables conservan actor, institución, acción, fecha y referencia. |
| **F0-07 · Módulos** | Las configuraciones solo PEI, solo POA y ambos funcionan según el contrato. Deshabilitar un módulo no afecta la consulta autorizada del otro. |
| **F0-08 · MFA** | Cada identidad de demostración debe completar el segundo factor en Keycloak. El recorrido debe comprobar que un código incorrecto no permite acceder. Registrar el resultado sin credenciales. |

Estos casos son el punto de partida de la matriz. El cierre de F0 requiere **cero accesos indebidos**, los dos recorridos de migración y evidencia de los permisos de todos los actores acordados.

## Decisiones aplicables

La [política vigente de F0](14-CONTRATO-F0.md#decisiones-operativas-para-el-cierre-de-f0) fija el MFA de las cuentas de demostración y las responsabilidades del Comité, el Director y Máxima Autoridad. La asignación de datos históricos requiere un destino explícito por PEI y por POA independiente. Un POA vinculado hereda la institución del PEI. La migración rechaza correspondencias ausentes o sobrantes antes de modificar los planes.

La [guía de ejecución](../../backend/README.md#arranque-local-de-f0) explica cómo preparar una instalación limpia y cómo actualizar datos anteriores. Para reproducir los casos HTTP y de navegador se utiliza una base desechable con los datos ficticios del repositorio.
