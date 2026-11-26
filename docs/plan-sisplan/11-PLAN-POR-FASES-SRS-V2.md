# Plan por fases de SISPLAN

**Base de trabajo · SRS v2 del reto PEI y POA · 26 de noviembre de 2026**

El plan cubre PEI y POA para la entrega de INTEC. Ambos módulos funcionan de forma autónoma y se pueden integrar cuando una institución usa los dos. Identidad, organización mínima, seguridad, auditoría, evidencias y reportes se desarrollan en la medida en que sostienen esos flujos. El presupuesto anual del POA es opcional y, cuando se registra, se presenta como un monto de referencia en RD$. El desarrollo de módulos de compras, contrataciones, pagos y presupuesto institucional queda fuera de la entrega académica.

Fuentes de aceptación: SRS v2, págs. 2–4 y 6–11, archivos Ejes y Objetivos DIGEMAPS y POA TI 2026 V2, y aclaración del maestro del 26 de noviembre. El [estado técnico y las pruebas ya ejecutadas](10-ADAPTACION-SRS-V2.md) sirven de punto de partida. El [plan anterior](01-PLAN-DE-CONSTRUCCION.md) conserva los antecedentes de la v1.

La secuencia de trabajo, la capacidad estimada para cinco personas y los cortes de aceptación están en el [Calendario de entrega de semanas 4 a 7](12-ENTREGA-SEMANA-7.md). El candidato debe estar listo al iniciar la semana 7.

**Subfases por fase**

- [F0 · Base institucional](#subfases-de-f0)
- [F1 · Carga y publicación del PEI](#subfases-de-f1)
- [F2 · Formulación y aprobación del POA](#subfases-de-f2)
- [F3 · Seguimiento y cierre del PEI](#subfases-de-f3)
- [F4 · Ejecución y cierre del POA](#subfases-de-f4)
- [F5 · Integración PEI–POA](#subfases-de-f5)
- [F6 · Aceptación y entrega](#subfases-de-f6)

Cada subfase indica las entradas necesarias para cerrar, el trabajo y la evidencia esperada. Los requisitos para preparar una subfase se indican cuando puede avanzar antes con datos de prueba. Las pruebas y el criterio de éxito de cada fase siguen debajo de su desglose.

## Dependencias y trabajo en paralelo

| Grupo | Fase | Qué necesita de otras fases | Qué habilita |
|---|---|---|---|
| Independiente | **F0 · Base institucional y contratos** | Puede empezar de inmediato. Usa el SRS, los archivos fuente y el PR actual. | Identidad, permisos, institución activa, catálogos, períodos, contratos y datos de prueba para ambos módulos. |
| Autónomas entre sí | **F1 · PEI: carga y publicación** | Contrato común de F0 para empezar y permisos, aislamiento y auditoría de F0 para cerrar. | PEI publicado y versionado, con líneas de acción, indicadores y metas consumibles por F3 y F5. |
| Autónomas entre sí | **F2 · POA: formulación y aprobación** | Contrato común de F0 para empezar y organización, permisos y aislamiento de F0 para cerrar. El POA autónomo puede completarse en paralelo con F1. | POA aprobado y publicable, con productos, actividades, metas, responsables y cronograma para F4 y F5. |
| Dependientes por módulo | **F3 · Seguimiento y cierre del PEI** | F1 entrega un PEI publicado, metas versionadas y permisos de revisión. Usa la auditoría de F0. Puede preparar los cálculos con datos de prueba mientras avanza F1. | Mediciones validadas, evaluación, reportes y cierre del PEI. |
| Dependientes por módulo | **F4 · Ejecución y cierre del POA** | F2 entrega un POA oficial, responsables asignados y programación aprobada. Usa la auditoría de F0 y puede preparar evidencias y cálculos en paralelo con F2. | Avances y evidencias validados, evaluación, reportes y cierre del POA. |
| Dependiente de ambos módulos | **F5 · Integración opcional PEI–POA** | Un PEI publicado de F1 y programación mínima de F2 para capturar la alineación en borrador. La aprobación de F2 fija el vínculo en la versión oficial. F3 y F4 habilitan los aportes al seguimiento. | Trazabilidad integrada y regresión de los dos modos autónomos. |
| Dependiente de todos los cierres | **F6 · Aceptación, despliegue y entrega** | Los cierres de F0–F5 habilitan la aceptación final. La matriz se prepara desde el inicio y el recorrido mínimo se ensaya en la semana 5. | Versión reproducible, demo y evidencias para la entrega. |

F1 y F2 pueden empezar con un contrato de institución, identidad y catálogos acordado en F0 y servicios de prueba. F3 y F4 siguen su propio módulo y pueden cerrarse en cualquier orden. F5 puede construir la alineación antes de aprobar el POA y completar la retroalimentación cuando F3 y F4 entreguen mediciones y avances validados. La administración institucional completa puede avanzar mientras se prueba la base mínima. Aislamiento y permisos deben estar activos desde el primer recorrido. Un bloqueo de la integración se registra en F5.

## F0 · Base institucional y contratos

**Trabajo y entregables.** Comparar el PR con el SRS v2. Definir el mínimo de instituciones, dependencias, usuarios, catálogos, períodos y módulos habilitados. Proteger el aislamiento en API y PostgreSQL mediante las políticas de filas previstas en el SRS. Acordar los contratos, la matriz de permisos, los estados y el registro de auditoría. Preparar dos instituciones ficticias, dependencias distintas, cuentas por rol y configuraciones de solo PEI, solo POA y ambos módulos. Migrar los datos existentes al ámbito institucional mediante una asignación verificable.

### Subfases de F0

La [preparación de F0](13-ARRANQUE-F0.md) recoge la línea base verificada, la secuencia inicial, los casos de QA y las decisiones pendientes.

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F0.1 · Alcance y contratos comunes** | SRS v2, archivos fuente y PR actual. | Confirmar rúbrica, rol del Comité, publicación y política de MFA. Acordar contratos de institución, sesión, catálogos, auditoría y [evidencias](12-ENTREGA-SEMANA-7.md#contrato-común-de-evidencias). Preparar datos de prueba para Angular y API. | Decisiones registradas con su fuente, contratos validados, casos obligatorios identificados y datos de prueba disponibles para F1 y F2. Resolver o escalar lo que afecte el plazo de entrega al cerrar la semana 4. |
| **F0.2 · Instituciones y organización** | F0.1. | Configurar las dos instituciones y dependencias del recorrido inicial. Completar catálogos, períodos y módulos habilitados. Ampliar la administración después del primer ensayo según la rúbrica. | Dos instituciones diferenciadas y las tres combinaciones de módulos comprobadas sin datos compartidos entre ellas. |
| **F0.3 · Identidad y permisos** | F0.1. Para probar el alcance institucional necesita F0.2. | Configurar cuentas de prueba, roles y permisos por institución y por dependencia. Documentar la resolución sobre Comité y publicación oficial. | La matriz rol–acción cubre los ocho actores del SRS y el rol técnico del Comité. Se comprueban accesos concedidos y denegados, sesión expirada y consulta del Auditor. |
| **F0.4 · Migración, aislamiento y auditoría** | F0.2 y F0.3. Se puede preparar la migración desde F0.1. | Aplicar políticas por institución en PostgreSQL y API. Asignar el ámbito de los datos anteriores y registrar los cambios que el contrato identifica como auditables. | Instalación limpia y actualización de datos previos verificadas. Reaplicar migraciones preserva los registros. Cero accesos indebidos en la batería entre instituciones y trazas completas en los casos auditables. |

### Pruebas de F0

| Área a revisar | Pruebas pertinentes |
|---|---|
| Identidad y permisos | Autenticación en Keycloak, cierre de sesión, expiración y ausencia de token. Probar cada permiso habilitado y su denegación con los ocho actores del SRS y el rol de Comité que hoy usa el flujo PEI. Verificar que el auditor solo pueda consultar y que los responsables vean el ámbito que les corresponde. Verificar MFA si forma parte de la configuración acordada. |
| Aislamiento | Con dos instituciones, intentar leer, crear, modificar y relacionar registros ajenos por ID desde la API. Verificar listados, búsquedas, auditoría y políticas de filas con la cuenta de aplicación. Repetir las pruebas de evidencia y reportes al habilitarlos en F3 y F4. |
| Organización, catálogos y módulos | Dependencia activa e inactiva, asignación de roles, catálogo compartido por módulo dentro de la institución y rutas deshabilitadas. Verificar que PEI y POA abran con el otro módulo deshabilitado y que una institución pueda habilitar ambos. |
| Contratos, auditoría y datos existentes | Validar OpenAPI, errores y cambios compatibles. Probar instalación limpia y actualización desde el PR actual con datos. Aplicar dos veces las migraciones. Comprobar que cada cambio de prueba conserva autor, fecha, entidad y acción según el contrato de auditoría. |

**Éxito.** Cero accesos indebidos en la matriz ejecutada con dos instituciones. El 100 % de los pares rol–acción declarados tiene un caso de autorización o denegación según corresponda. Pasan las tres configuraciones modulares y los dos recorridos de migración. Las decisiones pendientes tienen fase, responsable y fecha de resolución acordada por el equipo.

## F1 · PEI · Carga y publicación

**Trabajo y entregables.** Completar la carga de la estructura estratégica, su consulta y los datos que hacen medibles los indicadores. Preparar el mapa de eje, objetivo, resultado, indicador, meta y línea de acción e incorporar los riesgos estratégicos asociados al objetivo conforme al alcance de carga que se acuerde. Cerrar el flujo de envío, revisión, observaciones, corrección, aprobación y publicación con historial de versiones. La carga DIGEMAPS y el recorrido de aprobación ya tienen una implementación parcial verificada en el PR. El arranque del reto utiliza estructura precargada, conforme a la pág. 9.

### Subfases de F1

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F1.1 · Períodos y carga DIGEMAPS** | F0.1. Validación institucional con F0.2 y F0.4. | Configurar períodos de cuatro y cinco años y conciliar el catálogo precargado con el archivo fuente. Probar reintentos y fallos de carga. | Coinciden seis ejes, veinte objetivos y veinte indicadores. Cero duplicados y cero registros parciales en las pruebas de reintento y fallos. |
| **F1.2 · Mapa estratégico y metas** | F1.1. | Mostrar el mapa estratégico y distinguir indicadores listos para medir de indicadores pendientes de configuración. Aplicar la [regla de publicación](12-ENTREGA-SEMANA-7.md#indicadores-y-publicación-del-pei). | La publicación valida estructura y referencias. Los indicadores pendientes quedan identificados y fuera del cálculo. Un indicador configurado de prueba conserva meta, frecuencia, unidad y método para el seguimiento. |
| **F1.3 · Revisión y aprobación** | F1.2, F0.3 y F0.4. | Terminar la revisión por rol, observaciones, correcciones, reenvío, aprobación y control de versiones. | El 100 % de las transiciones acordadas tiene prueba positiva y denegación pertinente. La versión obsoleta se rechaza, y motivo y autor se conservan. |
| **F1.4 · Publicación autónoma** | F1.3 y F0.2. | Publicar, consultar el PEI oficial y su historial y ejecutar el recorrido con solo PEI habilitado. | Un PEI publicado consultable, su versión disponible y cero POA generados al publicar o repetir la acción. |

### Pruebas de F1

| Área a revisar | Pruebas pertinentes |
|---|---|
| Carga y fidelidad de DIGEMAPS | Cotejar las 20 filas fuente con los seis ejes, veinte objetivos y veinte indicadores cargados. Revisar textos, vínculos, tildes, campos vacíos y reintento sin duplicados. Probar fallo de carga y verificar que la transacción no deje datos parciales. |
| Vigencia y estructura | Horizontes de cuatro y cinco años, límites de fechas, solapamiento según la regla acordada, ejes y objetivos inactivos, riesgos asociados al objetivo, referencias válidas y campos de indicador pendientes. Verificar validaciones en interfaz y API. |
| Revisión, publicación y versiones | Analista → Director de Planificación → Comité → Máxima Autoridad y publicación según los permisos acordados. Probar observación con motivo, corrección y reenvío, intento de saltar pasos, rol incorrecto, revisión concurrente, versión obsoleta, repetición de publicación e historial consultable. |
| Autonomía e interfaz | Completar la publicación en una institución con solo PEI. Verificar catálogo, filtros, mapa, estados vacíos y errores. Confirmar que publicar el PEI no crea POA automáticamente. |

**Éxito.** Las 20 filas cotejadas coinciden con el catálogo de referencia y el reintento agrega cero duplicados. El 100 % de las transiciones vigentes tiene una prueba con el rol permitido y una denegación pertinente. El recorrido autónomo termina en un PEI publicado y consultable, con la versión y los motivos de devolución visibles.

## F2 · POA · Formulación y aprobación

**Trabajo y entregables.** Ampliar la apertura anual que ya existe con productos, actividades y tareas de la programación, metas físicas, unidad, indicadores operativos, dependencias, responsables y cronograma. Completar revisión de área, consolidación de Planificación y aprobación institucional por Máxima Autoridad conforme al flujo y la RACI. Publicar la versión aprobada. Mantener el presupuesto de referencia opcional y registrar el tratamiento de los campos faltantes del Excel.

### Subfases de F2

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F2.1 · Apertura y presupuesto opcional** | F0.1 para preparar. F0.2–F0.4 para certificar la apertura por institución. | Completar el alta anual autónoma, reglas de duplicados y presupuesto estimado opcional en RD$. | Pasan la apertura sin PEI y los casos campo omitido, nulo, cero y con centavos. La recarga conserva el monto exacto o muestra «Sin registrar». |
| **F2.2 · Productos, actividades y programación** | F2.1 y catálogos de F0.2. | Entregar [tres cortes comprobables](12-ENTREGA-SEMANA-7.md#primeros-cortes-del-poa). F2.2a crea producto y actividad. F2.2b agrega meta, unidad, responsable y fechas. F2.2c completa programación exigida y concilia el Excel. | F2.2b habilita el primer ensayo de aprobación. Al cerrar F2.2c, los 11 registros tienen mapeo o tratamiento documentado y pasan los casos de programación exigidos. |
| **F2.3 · Revisión de área y consolidación** | F2.2b y permisos de F0.3 para el primer caso. El cierre de F2 requiere F2.2c en el alcance obligatorio. | Enviar a revisión de área, registrar devoluciones y corregir. Consolidar con Planificación y versionar los cambios. | El flujo conserva motivo, ámbito y versión. Pasan el rechazo por dependencia, el reintento y la revisión concurrente. |
| **F2.4 · Aprobación y publicación autónoma** | F2.3 y auditoría de F0.4. | Aplicar la aprobación institucional y publicar la versión oficial. Comprobar la entrega del estado de entrada para F4. | Se logra publicar con y sin presupuesto informado en una institución con solo POA. Los estados inválidos o fuera de orden quedan bloqueados en API. |

### Pruebas de F2

| Área a revisar | Pruebas pertinentes |
|---|---|
| Formulación y datos fuente | Crear un POA en una institución con solo POA. Cotejar los 11 registros de referencia de la hoja Matriz POA y documentar los campos vacíos, los rótulos repetidos y las metas con unidades distintas. Probar productos con y sin actividades detalladas si la regla admite ambos casos, fechas en los límites del año y responsables de otra dependencia. |
| Presupuesto de referencia | Crear y avanzar en el flujo con campo omitido, nulo, cero explícito y monto con centavos. Verificar coma decimal en el formulario, recarga, persistencia exacta y rechazo de negativos o exceso de decimales. Comprobar que la ausencia de monto permite enviar, aprobar y publicar. Revisar que el estimado se identifique como referencia en cada vista y reporte donde aparezca. |
| Aprobación y programación | Devolución con motivo, corrección, rechazo, aprobación de área, consolidación, aprobación institucional y publicación. Probar permisos por dependencia, borrador incompleto, publicación prematura, dos envíos simultáneos, reintento y consulta de versiones. |
| Experiencia y contrato | Validar guardado, recuperación de errores, tablas, calendario y navegación por teclado de formularios. Revisar el contrato y errores de creación y actualización contra PostgreSQL. |

**Éxito.** Los 11 registros fuente tienen un mapeo revisado o una decisión de tratamiento documentada. El POA autónomo llega a publicación con y sin presupuesto informado. El 100 % de las transiciones acordadas queda cubierto por pruebas de permisos y estados, con trazabilidad a la versión aprobada.

## F3 · Seguimiento y cierre del PEI

**Trabajo y entregables.** Habilitar mediciones y evidencias por objetivo e indicador. Implementar evaluación y ajustes versionados, mapa y tablero de avance por eje y objetivo, informes por período y cierre con resultados y lecciones aprendidas. Acordar los métodos de cálculo antes de implementar agregados.

### Subfases de F3

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F3.1 · Períodos y reglas de medición** | F0.1 y contrato de indicadores de F1.2. Puede empezar con datos de prueba. | Definir períodos y reglas de cálculo y agregación por método. Preparar [casos con resultados esperados](12-ENTREGA-SEMANA-7.md#cálculos-de-referencia) para cantidades, proporciones y métricas donde un valor menor cumple la meta. | Casos con datos y resultados esperados, revisados para las cuatro frecuencias, incluidas base nula o cero, unidad incompatible y valor por encima o debajo del umbral. |
| **F3.2 · Registro y validación de mediciones** | F3.1, PEI publicado de F1.4, aislamiento de F0.4 y contrato común de evidencias acordado en F0.1. | Registrar y validar mediciones y evidencias con la meta, versión y permisos que correspondan. Usar la operación común de registro y consulta de evidencias. Las variantes específicas del POA pueden seguir en paralelo. | Pasa un recorrido por cada frecuencia. También se cubren duplicado, fechas límite, evidencia ajena y precisión decimal. |
| **F3.3 · Evaluación y ajustes** | F3.2. | Evaluar por eje y objetivo, devolver o corregir mediciones y aplicar ajustes con historial. | El 100 % de la muestra de cálculo coincide con el resultado esperado. Una corrección conserva las mediciones y evaluaciones anteriores en su versión. |
| **F3.4 · Informes y cierre autónomo** | F3.3. | Conciliar tablero e informes, registrar resultados y lecciones aprendidas y cerrar el PEI. | El reporte coincide con el tablero para el mismo período y versión. El historial se consulta después del cierre con solo PEI habilitado. |

### Pruebas de F3

| Área a revisar | Pruebas pertinentes |
|---|---|
| Medición y períodos | Registro y validación mensual, trimestral, semestral y anual. Probar inicio y fin de período, cambio de año, superposición y duplicados, indicador con datos pendientes, meta cero, distintas unidades, valores por encima de la meta y precisión decimal. Probar la regla aprobada para cada método de agregación. |
| Evidencias, evaluación y cambios | Evidencia asociada a la medición correcta, acceso por rol, devolución y corrección con historial, revisión de metas, ediciones concurrentes, cierre y consulta posterior. Verificar formatos y acceso de las evidencias usadas por el PEI. Probar un cambio de versión sin alterar los resultados históricos. |
| Tablero e informes | Conciliar detalle, agregado por objetivo o eje y reporte para la misma institución, período y versión. Probar filtros, combinaciones sin datos y permisos. Ejecutar todo el flujo de seguimiento y cierre con el módulo POA deshabilitado. |

**Éxito.** Un recorrido completo de medición, validación y consulta por cada una de las cuatro frecuencias. El 100 % de los resultados del conjunto de cálculo de referencia coincide con el valor esperado y concilia con el informe. Un PEI autónomo llega a cierre con historial recuperable.

## F4 · Ejecución y cierre del POA

**Trabajo y entregables.** Registrar avances por actividad y meta, adjuntar evidencias, dar seguimiento a plazos y validar avances. Implementar evaluación por período, ajustes de programación aprobados, reporte operativo y cierre anual. Definir el tratamiento de avances sin datos, metas en cero y reprogramaciones. Cuando una vista incluya el presupuesto de referencia, debe mantener esa etiqueta.

### Subfases de F4

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F4.1 · Reglas de avance y evaluación** | F0.1 y contrato del corte F2.2b. Puede empezar con datos de prueba. | Acordar captura, evaluación, unidades y períodos. Usar los casos de cálculo comunes con F3.1 para cantidades y porcentajes, con pruebas adicionales de sobrecumplimiento y doble conteo. | Los resultados esperados cubren cantidades, porcentajes y los cuatro períodos aplicables, con casos límite de la regla acordada. |
| **F4.2 · Evidencias y permisos** | Contrato común de evidencias de F0.1 y permisos y aislamiento de F0.3–F0.4. La validación contra una actividad requiere F2.2a. Puede avanzar en paralelo con F4.1. | Implementar adjuntos, enlaces, consulta, reemplazo e historial sobre la operación común de evidencias. La primera ruta compartida puede verificarse con una URL. | Pasan los formatos permitidos, el rechazo de entradas inválidas y la recuperación de fallos de carga. Cero lecturas o descargas indebidas en las pruebas. |
| **F4.3 · Ejecución y validación de avances** | F2.4, F4.1 y F4.2. | Registrar, validar y corregir avances con evidencias. Mostrar metas, plazos y estado de ejecución. | Se verifican cálculos, permisos y edición concurrente. El avance validado conserva el vínculo a actividad, período y evidencia. |
| **F4.4 · Reprogramación, informes y cierre autónomo** | F4.3. | Aprobar una reprogramación, preservar cortes anteriores, conciliar resultados y cerrar el año. | El informe coincide con los registros aprobados. Un POA autónomo sin presupuesto completa ejecución, evaluación y cierre. |

### Pruebas de F4

| Área a revisar | Pruebas pertinentes |
|---|---|
| Avances y cálculos | Registrar, validar y corregir avance con los períodos mensual, trimestral, semestral y anual que correspondan. Probar meta cero o ausente, unidad incompatible, cantidades y porcentajes, sobrecumplimiento, precisión, doble conteo y un avance fuera de período. Conciliar resultado por actividad, producto y reporte. |
| Evidencias | Probar PDF, Excel, imagen, acta o contrato como documento y URL. Verificar tipos y límites acordados, archivos con extensión incorrecta, carga interrumpida, enlace inválido, recuperación y reemplazo con historial. Comprobar denegación a usuarios sin permiso y a otra institución, incluso con el identificador o el enlace. |
| Reprogramación y cierre | Probar ajuste aprobado con versión y motivo, consulta de cortes anteriores, rechazo de un ajuste inválido, cierre anual, bloqueo de ediciones según permisos y reintento. Comprobar avance y evaluación sin presupuesto de referencia. |
| Experiencia e informes | Revisar actividad, metas y evidencias en móvil y escritorio, acceso por teclado, etiquetas, filtros y vistas sin información. Conciliar informe, tablero y registros para los mismos filtros. Completar el recorrido sin módulo PEI. |

**Éxito.** Un POA autónomo termina el ciclo de ejecución, evaluación y cierre. Coincide el 100 % de los resultados de la batería de cálculo y conciliación. Pasan todos los formatos de evidencia permitidos del conjunto de prueba, con cero accesos indebidos.

## F5 · Integración opcional PEI–POA

**Trabajo y entregables.** Partir del vínculo anual ya implementado. Agregar la relación entre líneas de acción y productos, y entre indicadores y metas cuando corresponda. Mostrar la trazabilidad desde POA hacia PEI y desde PEI hacia POA. Incorporar al seguimiento estratégico el aporte validado de avances y evidencias, con una regla explícita de correspondencia y medición.

### Subfases de F5

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F5.1 · Contrato de integración** | F0.1, estructura estratégica de F1.2 y programación mínima de F2.2b. Puede diseñarse con datos de prueba. | Acordar las referencias línea–producto e indicador–meta, la vigencia y el comportamiento cuando solo hay un módulo. | Los contratos cubren modos autónomos, vínculos válidos e inválidos y reglas de versión y pertenencia institucional. |
| **F5.2 · Alineación de planes** | F5.1, PEI publicado de F1.4 y POA en borrador con F2.2b para registrar la alineación. F2.4 permite verificarla en la versión oficial. | Vincular opcionalmente el producto en el borrador del POA a la línea de acción del PEI publicado. Conservar la referencia al aprobar. Para cambiarla después, crear una versión nueva y someterla a aprobación. | El vínculo persiste en ambos módulos tras aprobar. Se rechazan PEI en borrador, período incompatible, duplicados e institución ajena. Los cambios posteriores conservan la versión anterior. |
| **F5.3 · Aporte de avances y evidencias** | F5.2, F3.3 y F4.3. | Aplicar la correspondencia de período, unidad y fórmula al aporte validado. Cuando una evidencia carezca de los datos necesarios para medir el indicador, conservarla y dejar el resultado sin cambio. | Un aporte válido es trazable hasta la actividad y evidencia. Un avance incompatible, pendiente o rechazado deja la medición intacta. Un aporte repetido se contabiliza una sola vez. |
| **F5.4 · Regresión de los tres modos** | F5.3 y cierres autónomos F3.4 y F4.4. | Ejecutar PEI autónomo, POA autónomo y recorrido integrado. Probar también un POA sin vínculo en una institución con ambos módulos. | Pasan los tres recorridos. Los modos autónomos se pueden usar con el otro módulo deshabilitado dentro de los permisos previstos. |

### Pruebas de F5

| Área a revisar | Pruebas pertinentes |
|---|---|
| Vigencia y alcance de vínculos | Probar PEI publicado y vigente para el año del POA, períodos parciales, PEI en borrador o fuera de período, referencias cruzadas por institución y duplicados. Verificar captura del vínculo antes de aprobar el POA, persistencia en ambas consultas y preservación de la versión oficial al cambiar la relación posteriormente. |
| Aporte al seguimiento | Probar medición con evidencia validada, avance pendiente o rechazado, unidades incompatibles, reintento, vínculo duplicado y corrección. Verificar ruta completa desde producto y actividad hasta línea, objetivo, indicador y evidencia. |
| Regresión modular | Repetir los recorridos de solo PEI y solo POA. En una institución con ambos módulos, crear también un POA sin vínculo. Probar deshabilitación o fallo temporal del otro módulo y verificar el acceso al flujo autónomo según el contrato. |
| Auditoría y conciliación | Comparar trazas, versiones y resultados de ambos módulos para un mismo caso. Verificar que la evaluación estratégica use el aporte autorizado una sola vez, según su método de medición. |

**Éxito.** Los tres recorridos de entrega pasan: solo PEI, solo POA y PEI–POA vinculado. Todas las relaciones del conjunto de prueba conservan institución, período y versión. Cero vínculos indebidos y cero aportes duplicados al volver a procesar el mismo avance.

## F6 · Aceptación, despliegue y entrega

**Trabajo y entregables.** Consolidar los resultados de QA, revisar el alcance con el equipo y preparar la versión final. Entregar el sitio o entorno acordado, instrucciones de arranque y demo, SRS o anexo de trazabilidad, diagramas de arquitectura, datos y procesos, y checklist con evidencias. Ensayar los tres recorridos y verificar los accesos antes de la presentación.

### Subfases de F6

| Subfase | Entrada para cerrar | Trabajo | Evidencia de cierre |
|---|---|---|---|
| **F6.1 · Matriz, casos y guiones** | Puede empezar con el SRS v2. Se consolida con los acuerdos de F0.1. | Preparar la matriz y los tres guiones. Ensayar el recorrido mínimo durante la semana 5 con la porción necesaria de F0, F2 y F4 y registrar el commit. | Cada criterio obligatorio tiene caso y resultado esperado. El primer ensayo con avance y evidencia queda registrado antes de la regresión final. |
| **F6.2 · Regresión y aceptación del candidato** | F6.1 y F0–F5 cerradas. | Ejecutar el catálogo contra un único commit. Revisar permisos, aislamiento, accesibilidad en móvil y escritorio y métricas de cálculo y rendimiento. | El 100 % de los criterios obligatorios tiene evidencia aprobada. Cero defectos críticos o altos abiertos. |
| **F6.3 · Despliegue y recuperación** | F6.1 y candidato identificado. Cierra con el mismo commit verificado en F6.2. | Verificar instalación, actualización, respaldo, restauración, sesión y pruebas rápidas en el entorno de entrega. | El equipo reproduce el entorno y recupera el respaldo. Las pruebas rápidas pasan después de instalar y restaurar. |
| **F6.4 · Paquete de entrega y ensayo** | F6.2 y F6.3. Los borradores de documentación avanzan desde F6.1. | Consolidar instructivo, trazabilidad, diagramas, evidencias y checklist. Ensayar la demo y verificar accesos y enlaces. | Paquete accesible, enlaces comprobados y tres recorridos de demo aprobados con el entorno y las cuentas acordados. |

### Pruebas de F6

| Área a revisar | Pruebas pertinentes |
|---|---|
| Aceptación y regresión | Ejecutar el catálogo de aceptación contra un único commit candidato con la base preparada para los tres modos. Hacer revisión cruzada con cuentas de los roles pertinentes y enlazar cada resultado al criterio del SRS v2. |
| Accesibilidad y diseño institucional | Probar escritorio y móvil, incluidos 320 px y 390 px. Revisar zoom al 200 %, teclado, foco visible, contraste, etiquetas y errores. Verificar en especial la legibilidad de períodos, montos y controles de evidencias, además de la apertura de evidencias. Ejecutar análisis automático y complementar con revisión manual. |
| Seguridad, estabilidad y operación | Repetir controles de autorización por rol y por institución, revisión de dependencias y configuración, y comprobar ausencia de credenciales en el repositorio. Verificar instalación limpia, migración, recuperación de respaldo, arranque, sesión y smoke test en el entorno de entrega. Medir los tiempos del listado de planes, guardado de un avance y generación del reporte con un volumen y una concurrencia acordados en F0. |
| Entregables y demo | Verificar enlaces y permisos de acceso, versiones, diagrama frente a lo implementado, pasos reproducibles y datos de prueba claramente identificados. Ensayar con las cuentas y equipos que se usarán en la entrega. |

**Éxito.** El 100 % de los criterios obligatorios del SRS v2 tiene evidencia aprobada y los tres recorridos de demo pasan. Cero defectos críticos o altos abiertos en el candidato. Se puede desplegar, probar y restaurar el entorno con las instrucciones entregadas.

## Métricas y registro de QA

| Métrica | Cómo se registra | Meta de salida |
|---|---|---|
| Cobertura de aceptación | Criterios obligatorios aprobados con evidencia ÷ criterios obligatorios aplicables del alcance congelado. Reportar por fase y para la entrega. | 100 % para cerrar cada fase. Un caso bloqueado o pendiente permanece en el denominador. |
| Resultados de pruebas | Conteo de aprobadas, fallidas, bloqueadas y sin ejecutar por tipo y por fase. Asociar regresiones a su incidente. | Todos los casos obligatorios ejecutados y aprobados. |
| Integridad y control de acceso | Número de errores de migración, fugas entre instituciones, operaciones indebidas, vínculos inválidos y duplicados en los escenarios de prueba. | Cero en el conjunto aprobado. |
| Exactitud y trazabilidad | Comparar cálculos y muestra de historial con datos fuente y resultados esperados. | 100 % de coincidencia en el conjunto de referencia. |
| Usabilidad y rendimiento | Registrar anchos, teclado, contraste, tiempos p95, entorno, volumen y concurrencia. Usar como punto de partida los datos de referencia y cinco sesiones simultáneas. Fijar en F0 los umbrales por operación con una medición inicial. | Recorridos principales utilizables en escritorio y móvil, cero bloqueos de accesibilidad en esos recorridos y umbral acordado cumplido. |

Registrar cada caso con ID, fase, página o decisión de origen, datos, rol, modo, pasos, resultado esperado y obtenido, commit, entorno, estado, severidad y evidencia. Antes de marcar una fase como cerrada, el responsable y otro integrante revisan sus criterios y enlazan el resultado al PR. Marcar como **bloqueada** la fase que no pueda demostrar un criterio obligatorio por una dependencia pendiente.

Combinar pruebas unitarias para reglas y cálculos, validación de contrato para Angular y API, integración con PostgreSQL y Keycloak, pruebas de navegador para recorridos y accesibilidad, y pruebas manuales de aceptación y demo. Mantener como regresión la base ya comprobada de 21 pruebas Angular, los scripts HTTP del PEI y POA y la migración sobre un POA existente. Las pruebas nuevas se agregan conforme se implementa cada fase.

### Escenarios mínimos de aceptación

| Caso | Datos de prueba | Resultado esperado |
|---|---|---|
| PEI autónomo | Institución A con módulo PEI, catálogo DIGEMAPS y roles habilitados | Carga, publicación, seguimiento y cierre trazables. El recorrido no requiere un POA. |
| POA autónomo | Institución B con módulo POA, una meta física de 100 unidades y 75 unidades validadas | El plan puede aprobarse con presupuesto vacío. Para un indicador de cumplimiento definido como avance dividido por meta, el resultado es 75 % en detalle, tablero y reporte. |
| POA vinculado | Institución A con ambos módulos habilitados, un PEI publicado y un POA con producto alineado a una línea de acción | Se puede navegar en ambos sentidos. Repetir el registro del mismo aporte conserva una sola medición conforme a la regla acordada. |
| Aislamiento | Instituciones A y B con datos y evidencias distintos, y cuentas institucionales independientes | Ninguna cuenta institucional accede a los registros de la otra. Repetir el intento en búsquedas, detalle, auditoría y descarga. |
| Presupuesto ausente y cero | Dos POA de períodos distintos, uno sin monto y otro con RD$ 0.00 declarado | El listado distingue «Sin registrar» de RD$ 0.00, y ambos pueden completar el flujo. |

## Decisiones que requieren validación

| Decisión | Cuándo resolverla | Qué afecta |
|---|---|---|
| Comité, publicación oficial, rúbrica y MFA | En F0.1 durante la semana 4. El flujo de la pág. 7 incluye Comité y la RACI de las págs. 2–3 omite ese rol. Confirmar permisos y qué perfiles requieren MFA. | Alcance de entrega, matriz de permisos y pruebas de sesión, aprobación y publicación. |
| Estados, publicación y modificaciones del POA | En F2, antes de implementar el paso a ejecución. Conciliar la revisión por área de la RACI con el flujo general de la pág. 8. | Contrato de estados, reprogramación, versionado y entrada de F4. |
| Indicadores, períodos y reportes | En F3 y F4, antes de fijar resultados de referencia. Resolver cálculo con meta cero o ausente, unidades, redondeo, cortes y reglas de consolidación. | Exactitud de tableros, informes y retroalimentación de F5. |
| Tratamiento de riesgos y actividades no planificadas | En F0, al delimitar qué datos del PEI se cargan y si alguna ampliación entra en la rúbrica. | Alcance, trazabilidad y orden del trabajo. |
| Rúbrica y formato de entrega | En F0, con el equipo y el aula. Verificar fecha y plataforma de entrega. | Criterios finales, paquete documental y ensayo de F6. |

El PR actual aporta evidencia parcial para F0 (identidad), F1 (carga y aprobación), F2 (apertura y presupuesto) y F5 (vínculo anual). El cierre de cada fase requiere toda su evidencia. Los acuerdos mínimos de F0 y los casos de aceptación de F1 y F2 se preparan en paralelo. Los cinco frentes pueden repartirse entre plataforma y seguridad, PEI, POA, UX y reportes, y QA y documentación. Las asignaciones se confirman con el equipo.
