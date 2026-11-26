# Plan por fases de SISPLAN

**Base de trabajo · SRS v2 del reto PEI y POA · 26 de noviembre de 2026**

El plan cubre PEI y POA para la entrega de INTEC. Ambos módulos funcionan de forma autónoma y se pueden integrar cuando una institución usa los dos. Identidad, organización mínima, seguridad, auditoría, evidencias y reportes se desarrollan en la medida en que sostienen esos flujos. El presupuesto anual del POA es opcional y, cuando se registra, se presenta como un monto de referencia en RD$. El desarrollo de módulos de compras, contrataciones, pagos y presupuesto institucional queda fuera de la entrega académica.

Fuentes de aceptación: SRS v2, págs. 2–4 y 6–11, archivos Ejes y Objetivos DIGEMAPS y POA TI 2026 V2, y aclaración del maestro del 26 de noviembre. El [estado técnico y las pruebas ya ejecutadas](10-ADAPTACION-SRS-V2.md) sirven de punto de partida. El [plan anterior](01-PLAN-DE-CONSTRUCCION.md) conserva los antecedentes de la v1.

## Dependencias y trabajo en paralelo

| Grupo | Fase | Qué necesita de otras fases | Qué habilita |
|---|---|---|---|
| Independiente | **F0 · Base institucional y contratos** | Puede empezar de inmediato. Usa el SRS, los archivos fuente y el PR actual. | Identidad, permisos, institución activa, catálogos, períodos, contratos y datos de prueba para ambos módulos. |
| Autónomas entre sí | **F1 · PEI: carga y publicación** | Contrato común de F0 para empezar y permisos, aislamiento y auditoría de F0 para cerrar. | PEI publicado y versionado, con líneas de acción, indicadores y metas consumibles por F3 y F5. |
| Autónomas entre sí | **F2 · POA: formulación y aprobación** | Contrato común de F0 para empezar y organización, permisos y aislamiento de F0 para cerrar. El POA autónomo puede completarse en paralelo con F1. | POA aprobado y publicable, con productos, actividades, metas, responsables y cronograma para F4 y F5. |
| Dependientes por módulo | **F3 · Seguimiento y cierre del PEI** | F1 entrega un PEI publicado, metas versionadas y permisos de revisión. Usa la auditoría de F0. Puede preparar los cálculos con datos de prueba mientras avanza F1. | Mediciones validadas, evaluación, reportes y cierre del PEI. |
| Dependientes por módulo | **F4 · Ejecución y cierre del POA** | F2 entrega un POA oficial, responsables asignados y programación aprobada. Usa la auditoría de F0 y puede preparar evidencias y cálculos en paralelo con F2. | Avances y evidencias validados, evaluación, reportes y cierre del POA. |
| Dependiente de ambos módulos | **F5 · Integración opcional PEI–POA** | F1 y F2 para validar la alineación de la programación. F3 y F4 para validar el aporte de avances y evidencias al seguimiento estratégico. | Trazabilidad integrada y regresión de los dos modos autónomos. |
| Dependiente de todos los cierres | **F6 · Aceptación, despliegue y entrega** | F0–F5 aprobadas para cerrar. La matriz, los guiones de prueba y el paquete de entrega pueden prepararse desde el inicio. | Versión reproducible, demo y evidencias para la entrega. |

F1 y F2 pueden empezar con un contrato de institución, identidad y catálogos acordado en F0 y servicios de prueba. F3 y F4 siguen su propio módulo y pueden cerrarse en cualquier orden. F5 puede construir la alineación al cerrar F1 y F2 y completar la retroalimentación al cerrar F3 y F4. Un bloqueo de la integración se registra en F5.

## F0 · Base institucional y contratos

**Trabajo y entregables.** Comparar el PR con el SRS v2. Definir el mínimo de instituciones, dependencias, usuarios, catálogos, períodos y módulos habilitados. Proteger el aislamiento en API y PostgreSQL mediante las políticas de filas previstas en el SRS. Acordar los contratos, la matriz de permisos, los estados y el registro de auditoría. Preparar dos instituciones ficticias, dependencias distintas, cuentas por rol y configuraciones de solo PEI, solo POA y ambos módulos. Migrar los datos existentes al ámbito institucional mediante una asignación verificable.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Identidad y permisos | Autenticación en Keycloak, cierre de sesión, expiración y ausencia de token. Probar cada permiso habilitado y su denegación con los ocho actores del SRS y el rol de Comité que hoy usa el flujo PEI. Verificar que el auditor solo pueda consultar y que los responsables vean el ámbito que les corresponde. Verificar MFA si forma parte de la configuración acordada. |
| Aislamiento | Con dos instituciones, intentar leer, crear, modificar y relacionar registros ajenos por ID desde la API. Verificar listados, búsquedas, auditoría y políticas de filas con la cuenta de aplicación. Repetir las pruebas de evidencia y reportes al habilitarlos en F3 y F4. |
| Organización, catálogos y módulos | Dependencia activa e inactiva, asignación de roles, catálogo compartido por módulo dentro de la institución y rutas deshabilitadas. Verificar que PEI y POA abran con el otro módulo deshabilitado y que una institución pueda habilitar ambos. |
| Contratos, auditoría y datos existentes | Validar OpenAPI, errores y cambios compatibles. Probar instalación limpia y actualización desde el PR actual con datos. Aplicar dos veces las migraciones. Comprobar que cada cambio de prueba conserva autor, fecha, entidad y acción según el contrato de auditoría. |

**Éxito.** Cero accesos indebidos en la matriz ejecutada con dos instituciones. El 100 % de los pares rol–acción declarados tiene un caso de autorización o denegación según corresponda. Pasan las tres configuraciones modulares y los dos recorridos de migración. Las decisiones pendientes tienen fase, responsable y fecha de resolución acordada por el equipo.

## F1 · PEI · Carga y publicación

**Trabajo y entregables.** Completar la carga de la estructura estratégica, su consulta y los datos que hacen medibles los indicadores. Preparar el mapa de eje, objetivo, resultado, indicador, meta y línea de acción e incorporar los riesgos estratégicos asociados al objetivo conforme al alcance de carga que se acuerde. Cerrar el flujo de envío, revisión, observaciones, corrección, aprobación y publicación con historial de versiones. La carga DIGEMAPS y el recorrido de aprobación ya tienen una implementación parcial verificada en el PR. El arranque del reto utiliza estructura precargada, conforme a la pág. 9.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Carga y fidelidad de DIGEMAPS | Cotejar las 20 filas fuente con los seis ejes, veinte objetivos y veinte indicadores cargados. Revisar textos, vínculos, tildes, campos vacíos y reintento sin duplicados. Probar fallo de carga y verificar que la transacción no deje datos parciales. |
| Vigencia y estructura | Horizontes de cuatro y cinco años, límites de fechas, solapamiento según la regla acordada, ejes y objetivos inactivos, riesgos asociados al objetivo, referencias válidas y campos de indicador pendientes. Verificar validaciones en interfaz y API. |
| Revisión, publicación y versiones | Analista → Director de Planificación → Comité → Máxima Autoridad y publicación según los permisos acordados. Probar observación con motivo, corrección y reenvío, intento de saltar pasos, rol incorrecto, revisión concurrente, versión obsoleta, repetición de publicación e historial consultable. |
| Autonomía e interfaz | Completar la publicación en una institución con solo PEI. Verificar catálogo, filtros, mapa, estados vacíos y errores. Confirmar que publicar el PEI no crea POA automáticamente. |

**Éxito.** Las 20 filas cotejadas coinciden con el catálogo de referencia y el reintento agrega cero duplicados. El 100 % de las transiciones vigentes tiene una prueba con el rol permitido y una denegación pertinente. El recorrido autónomo termina en un PEI publicado y consultable, con la versión y los motivos de devolución visibles.

## F2 · POA · Formulación y aprobación

**Trabajo y entregables.** Ampliar la apertura anual que ya existe con productos, actividades y tareas de la programación, metas físicas, unidad, indicadores operativos, dependencias, responsables y cronograma. Completar revisión de área, consolidación de Planificación y aprobación institucional por Máxima Autoridad conforme al flujo y la RACI. Publicar la versión aprobada. Mantener el presupuesto de referencia opcional y registrar el tratamiento de los campos faltantes del Excel.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Formulación y datos fuente | Crear un POA en una institución con solo POA. Cotejar los 11 registros de referencia de la hoja Matriz POA y documentar los campos vacíos, los rótulos repetidos y las metas con unidades distintas. Probar productos con y sin actividades detalladas si la regla admite ambos casos, fechas en los límites del año y responsables de otra dependencia. |
| Presupuesto de referencia | Crear y avanzar en el flujo con campo omitido, nulo, cero explícito y monto con centavos. Verificar coma decimal en el formulario, recarga, persistencia exacta y rechazo de negativos o exceso de decimales. Comprobar que la ausencia de monto permite enviar, aprobar y publicar. Revisar que el estimado se identifique como referencia en cada vista y reporte donde aparezca. |
| Aprobación y programación | Devolución con motivo, corrección, rechazo, aprobación de área, consolidación, aprobación institucional y publicación. Probar permisos por dependencia, borrador incompleto, publicación prematura, dos envíos simultáneos, reintento y consulta de versiones. |
| Experiencia y contrato | Validar guardado, recuperación de errores, tablas, calendario y navegación por teclado de formularios. Revisar el contrato y errores de creación y actualización contra PostgreSQL. |

**Éxito.** Los 11 registros fuente tienen un mapeo revisado o una decisión de tratamiento documentada. El POA autónomo llega a publicación con y sin presupuesto informado. El 100 % de las transiciones acordadas queda cubierto por pruebas de permisos y estados, con trazabilidad a la versión aprobada.

## F3 · Seguimiento y cierre del PEI

**Trabajo y entregables.** Habilitar mediciones y evidencias por objetivo e indicador. Implementar evaluación y ajustes versionados, mapa y tablero de avance por eje y objetivo, informes por período y cierre con resultados y lecciones aprendidas. Acordar los métodos de cálculo antes de implementar agregados.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Medición y períodos | Registro y validación mensual, trimestral, semestral y anual. Probar inicio y fin de período, cambio de año, superposición y duplicados, indicador con datos pendientes, meta cero, distintas unidades, valores por encima de la meta y precisión decimal. Probar la regla aprobada para cada método de agregación. |
| Evidencias, evaluación y cambios | Evidencia asociada a la medición correcta, acceso por rol, devolución y corrección con historial, revisión de metas, ediciones concurrentes, cierre y consulta posterior. Verificar formatos y acceso de las evidencias usadas por el PEI. Probar un cambio de versión sin alterar los resultados históricos. |
| Tablero e informes | Conciliar detalle, agregado por objetivo o eje y reporte para la misma institución, período y versión. Probar filtros, combinaciones sin datos y permisos. Ejecutar todo el flujo de seguimiento y cierre con el módulo POA deshabilitado. |

**Éxito.** Un recorrido completo de medición, validación y consulta por cada una de las cuatro frecuencias. El 100 % de los resultados del conjunto de cálculo de referencia coincide con el valor esperado y concilia con el informe. Un PEI autónomo llega a cierre con historial recuperable.

## F4 · Ejecución y cierre del POA

**Trabajo y entregables.** Registrar avances por actividad y meta, adjuntar evidencias, dar seguimiento a plazos y validar avances. Implementar evaluación por período, ajustes de programación aprobados, reporte operativo y cierre anual. Definir el tratamiento de avances sin datos, metas en cero y reprogramaciones. Cuando una vista incluya el presupuesto de referencia, debe mantener esa etiqueta.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Avances y cálculos | Registrar, validar y corregir avance con los períodos mensual, trimestral, semestral y anual que correspondan. Probar meta cero o ausente, unidad incompatible, cantidades y porcentajes, sobrecumplimiento, precisión, doble conteo y un avance fuera de período. Conciliar resultado por actividad, producto y reporte. |
| Evidencias | Probar PDF, Excel, imagen, acta o contrato como documento y URL. Verificar tipos y límites acordados, archivos con extensión incorrecta, carga interrumpida, enlace inválido, recuperación y reemplazo con historial. Comprobar denegación a usuarios sin permiso y a otra institución, incluso con el identificador o el enlace. |
| Reprogramación y cierre | Probar ajuste aprobado con versión y motivo, consulta de cortes anteriores, rechazo de un ajuste inválido, cierre anual, bloqueo de ediciones según permisos y reintento. Comprobar avance y evaluación sin presupuesto de referencia. |
| Experiencia e informes | Revisar actividad, metas y evidencias en móvil y escritorio, acceso por teclado, etiquetas, filtros y vistas sin información. Conciliar informe, tablero y registros para los mismos filtros. Completar el recorrido sin módulo PEI. |

**Éxito.** Un POA autónomo termina el ciclo de ejecución, evaluación y cierre. Coincide el 100 % de los resultados de la batería de cálculo y conciliación. Pasan todos los formatos de evidencia permitidos del conjunto de prueba, con cero accesos indebidos.

## F5 · Integración opcional PEI–POA

**Trabajo y entregables.** Partir del vínculo anual ya implementado. Agregar la relación entre líneas de acción y productos, y entre indicadores y metas cuando corresponda. Mostrar la trazabilidad desde POA hacia PEI y desde PEI hacia POA. Incorporar al seguimiento estratégico el aporte validado de avances y evidencias, con una regla explícita de correspondencia y medición.

| Área a revisar | Pruebas pertinentes |
|---|---|
| Vigencia y alcance de vínculos | Probar PEI publicado y vigente para el año del POA, períodos parciales, PEI en borrador o fuera de período, referencias cruzadas por institución y duplicados. Validar el vínculo con ambas consultas y conservar su contexto al publicar nuevas versiones. |
| Aporte al seguimiento | Probar medición con evidencia validada, avance pendiente o rechazado, unidades incompatibles, reintento, vínculo duplicado y corrección. Verificar ruta completa desde producto y actividad hasta línea, objetivo, indicador y evidencia. |
| Regresión modular | Repetir los recorridos de solo PEI y solo POA. En una institución con ambos módulos, crear también un POA sin vínculo. Probar deshabilitación o fallo temporal del otro módulo y verificar el acceso al flujo autónomo según el contrato. |
| Auditoría y conciliación | Comparar trazas, versiones y resultados de ambos módulos para un mismo caso. Verificar que la evaluación estratégica use el aporte autorizado una sola vez, según su método de medición. |

**Éxito.** Los tres recorridos de entrega pasan: solo PEI, solo POA y PEI–POA vinculado. Todas las relaciones del conjunto de prueba conservan institución, período y versión. Cero vínculos indebidos y cero aportes duplicados al volver a procesar el mismo avance.

## F6 · Aceptación, despliegue y entrega

**Trabajo y entregables.** Consolidar los resultados de QA, revisar el alcance con el equipo y preparar la versión final. Entregar el sitio o entorno acordado, instrucciones de arranque y demo, SRS o anexo de trazabilidad, diagramas de arquitectura, datos y procesos, y checklist con evidencias. Ensayar los tres recorridos y verificar los accesos antes de la presentación.

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
| Comité en el PEI y publicación oficial | En F0, antes de cerrar F1. El flujo de la pág. 7 incluye Comité y la RACI de las págs. 2–3 omite ese rol. | Matriz de permisos y pruebas de aprobación y publicación. |
| Estados, publicación y modificaciones del POA | En F2, antes de implementar el paso a ejecución. Conciliar la revisión por área de la RACI con el flujo general de la pág. 8. | Contrato de estados, reprogramación, versionado y entrada de F4. |
| Indicadores, períodos y reportes | En F3 y F4, antes de fijar resultados de referencia. Resolver cálculo con meta cero o ausente, unidades, redondeo, cortes y reglas de consolidación. | Exactitud de tableros, informes y retroalimentación de F5. |
| Tratamiento de riesgos y actividades no planificadas | En F0, al delimitar qué datos del PEI se cargan y si alguna ampliación entra en la rúbrica. | Alcance, trazabilidad y orden del trabajo. |
| Rúbrica y formato de entrega | En F0, con el equipo y el aula. Verificar fecha y plataforma de entrega. | Criterios finales, paquete documental y ensayo de F6. |

El PR actual aporta evidencia parcial para F0 (identidad), F1 (carga y aprobación), F2 (apertura y presupuesto) y F5 (vínculo anual). El cierre de cada fase requiere toda su evidencia. Los acuerdos mínimos de F0 y los casos de aceptación de F1 y F2 se preparan en paralelo. Los cinco frentes pueden repartirse entre plataforma y seguridad, PEI, POA, UX y reportes, y QA y documentación. Las asignaciones se confirman con el equipo.
