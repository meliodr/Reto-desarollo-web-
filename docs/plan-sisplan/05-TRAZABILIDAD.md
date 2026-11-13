# Matriz de trazabilidad de SISPLAN

Base funcional: SRS SISPLAN v1.0, 11 páginas, y consigna del Reto Web PEI-POA. Los nombres de pantalla, la asignación a tareas y los criterios de aceptación concretan el desarrollo propuesto. Cada criterio requiere evidencia del producto institucional al ejecutarse. El repositorio consultado contiene documentación de preparación y registra el producto como sin iniciar. [Estado del proyecto en el commit revisado](https://github.com/meliodr/Reto-desarollo-web-/blob/b1d7586d7e87e6544fc40e600551b7ba5fe6751a/docs/ESTADO.md)

**Referencias de datos.** `Ejes` identifica la hoja `Hoja1` de **Ejes y Objetivos DIGEMAPS (1).xlsx**. `POA` identifica la hoja `Matriz POA` de **POA TI 2026 V2.xlsx**. Las ubicaciones de la matriz corresponden a esos archivos. Los escenarios con valores de ejecución se generan para las pruebas y se registran como datos de demostración.

## Requisitos funcionales

Los IDs coinciden con las tareas directas del portal. La columna de aceptación describe el resultado que debe observar la persona revisora en la aplicación y, cuando aplica una restricción, también en la API.

| ID y página | Requisito del SRS | Pantalla y tarea | Aceptación propuesta | Entrada Excel |
|---|---|---|---|---|
| RF-001 · p. 5 | Crear PEI con nombre, período, inicio, fin, misión, visión, valores y estado. | Planificación estratégica › Formulario de PEI · RF-001 | Crear el plan con todos sus campos, recargar y comprobar que se conservan. Verificar el período configurable y recorrer aprobación y publicación según los permisos de CU-001. | El catálogo aporta la estructura de ejes y objetivos. Nombre del plan, misión, visión, valores y vigencia deben definirse en el formulario. |
| RF-002 · p. 5 | Crear, modificar, inactivar y versionar ejes. | Detalle PEI › Ejes e historial · RF-002 y T-05 | Crear y modificar un eje, consultar ambas versiones e inactivarlo. Comprobar que se conserva la relación histórica con registros previos. | Ejes A4:A23. Seis nombres en celdas combinadas. El SRS enumera cinco ejemplos. |
| RF-003 · p. 6 | Registrar código, nombre, descripción, resultado esperado y línea de acción del objetivo. | Detalle de eje › Objetivos · RF-003 | Crear un objetivo en un eje, volver a abrirlo y verificar sus cinco campos y la relación. Al separar el texto original del título y la descripción, conservar la fuente. | Ejes B4:B23, C4:C23 y E4:E23. Los números del texto se repiten entre ejes, por lo que cada objetivo requiere un identificador interno propio. |
| RF-004 · p. 6 | Definir nombre, fórmula, unidad, línea base, meta y frecuencia del indicador estratégico. | Detalle de objetivo › Ficha del indicador · RF-004 | Guardar y consultar los seis campos. Registrar una medición con su período y comprobar que la fórmula, la unidad y la meta quedan identificadas. | Ejes D4:D23 aporta nombres. Fórmula, unidad explícita, línea base, meta y frecuencia se completan al configurar. |
| RF-005 · pp. 3 y 6 | Generar automáticamente un POA por año del PEI. | PEI › POA generados · RF-005 | Para un PEI de cinco años 2026–2030, generar exactamente cinco POA, cada uno con año y vínculo. Repetir el disparador acordado y comprobar que no se duplican. Aplicar RN-001 al definir la generación y la vigencia. | POA C5 indica 2026 y B7 identifica el área TI. Los otros años del ejemplo provienen del SRS. |
| RF-006 · p. 6 | Asociar cada producto operativo a eje, objetivo e indicador estratégico. | POA › Producto › Alineación estratégica · RF-006 | Guardar un producto con los tres vínculos coherentes dentro del PEI y navegar hasta ellos. Impedir completar el producto con vínculos ausentes o pertenecientes a otra rama estratégica. | POA B12:B22 aporta solo el eje 4. Contrastar con Ejes A15:A17 y acordar objetivo e indicador por producto. |
| RF-007 · p. 6 | Gestionar producto/descripción, objetivo, unidad y responsable. | POA › Matriz de productos y formulario · RF-007 | Guardar y consultar los campos y la dependencia. Cargar como registros distintos las dos filas con el rótulo «Producto 8». Conservar el estado y la revisión del planificador definidos por el flujo acordado. | POA C12:C22, E12:E22 y V12:V22. F y G aportan verificación esperada e involucrados. |
| RF-008 · pp. 6–7 | Crear actividades con cronograma y responsable. | Producto › Actividades y cronograma · RF-008 | Registrar una actividad en el producto y consultar su programación y responsable. Rechazar la creación sin responsable y comprobar el manejo de una actividad vencida. | POA D12:D13 contiene actividades explícitas. D14:D22 está vacío. V identifica un cargo genérico de seguimiento. |
| RF-009 · p. 7 | Registrar meta anual, programación mensual y programación cuatrimestral. | Producto › Programación de metas · RF-009 | Consultar doce meses y tres cuatrimestres. Para infraestructura virtual verificar meta anual 8, con 5 unidades en enero–abril y 3 en mayo–agosto. Para licencias, verificar 90 % anual con la regla aditiva de esa fila y preservar los meses vacíos. | POA H12:T22. I10:T10 agrupa cuatro trimestres, mientras el SRS define tres cuatrimestres. |
| RF-010 · p. 7 | Registrar avance físico, financiero, comentarios y evidencias. | Actividad › Registrar avance · RF-010 | Guardar avance físico y financiero por período, comentario y evidencia. Verificar persistencia, responsable, unidad, moneda y relación con actividad y meta. Probar RN-006 en el campo que el equipo defina como porcentaje de avance. | POA H:T contiene metas programadas. U12:U22 está vacío. La ejecución, costos reales y comentarios de prueba se agregan como datos de demostración. |
| RF-011 · p. 7 | Almacenar PDF, Word, Excel, imágenes y enlaces. | Avance › Adjuntos y Evidencias › Detalle · RF-011 y T-05 | Adjuntar y recuperar un ejemplo de cada formato desde el registro asociado. Comprobar los permisos de consulta y el historial de una sustitución conforme al versionado acordado. | POA F12:F22 describe evidencia esperada, por ejemplo informe de servidores o lista de asistencia. Los adjuntos del escenario deben prepararse. |
| RF-012 · pp. 3 y 7 | Generar tres evaluaciones por POA, enero–abril, mayo–agosto y septiembre–diciembre. | POA › Evaluaciones · RF-012 | Verificar exactamente los tres períodos, sin duplicados al repetir la generación. Comprobar que los avances de abril y mayo caen en evaluaciones distintas y que RN-005 impide finalizar sin evidencias. | POA I:T permite preparar metas por corte. Los encabezados de trimestre sirven como referencia de meses. |
| RF-013 · p. 7 | Calcular cumplimiento como ejecutado dividido por programado, multiplicado por 100. | Evaluación › Resultado y detalle del cálculo · RF-013 | Con programado 20 y ejecutado 15, obtener 75 % e identificar período, unidad y versión de la meta. Con denominador cero, mostrar el estado acordado y conservar ambos valores. Ejecutar casos de sobreejecución después de fijar la política. | POA H:T aporta el denominador programado cuando corresponda. El numerador se registra en el sistema. |
| RF-014 · p. 8 | Semáforo verde desde 90 %, amarillo de 70 % a 89 % y rojo por debajo de 70 %. | Evaluación y Dashboard › Estado de cumplimiento · RF-014 | Probar rojo al 69 %, amarillo al 70 % y 89 %, verde al 90 %. Mostrar etiqueta textual además del color. Añadir casos próximos a los umbrales al acordar redondeo y decimales. | Metas de POA H:T con avances de prueba, o valores de prueba independientes para umbrales. |
| RF-015 · p. 8 | Solicitar plan de acción correctiva cuando el cumplimiento sea inferior a 70 %. | Evaluación › Plan correctivo · RF-015 | Evaluar al 69 % y verificar la obligación vinculada a la evaluación. Evaluar al 70 % y verificar que el umbral no la activa. Registrar un plan con responsable y seguimiento si se aprueban esos campos. | Resultado de prueba calculado con RF-013. |
| RF-016 · pp. 8–10 | Mostrar PEI/POA vigentes y cumplimiento institucional, por eje, área e indicador. | Dashboard ejecutivo · RF-016 | Verificar las seis salidas contra registros de prueba. Abrir el detalle hasta la evaluación de origen y comprobar que cifras y filtros coinciden con la regla de consolidación acordada. | Ejes A:D y POA B:H aportan estructura, objetivos, productos y metas. La ejecución se registra aparte. |
| RF-017 · pp. 8 y 10 | Generar informes PEI, POA, cuatrimestral, anual, indicadores y presupuesto. | Reportes › Catálogo, filtros y descarga · RF-017 | Generar los seis informes y contrastar período, alcance, totales y fuentes con la aplicación. Para datos financieros ausentes, mostrar su condición pendiente. Verificar PDF y Excel, formatos recomendados en p. 10 y adoptados en la propuesta de construcción. | Ejes A:E y POA B:V para estructura y programación. La información de ejecución se genera con avances de prueba. |
| RF-018 · p. 8 | Notificar evaluaciones pendientes, actividades vencidas, indicadores sin actualizar e incumplimientos. | Centro de alertas · RF-018 | Preparar un caso de cada condición y comprobar evento, registro enlazado y destinatario acordado. El vencimiento debe generar alerta automáticamente según RN-007. | POA cronograma y Ejes catálogo orientan el escenario. Vencimientos, mediciones y destinatarios se configuran en el sistema. |

## Reglas de negocio y requisitos no funcionales

| ID y página | Regla | Pantalla y tarea | Aceptación propuesta y datos |
|---|---|---|---|
| RN-001 · p. 8 | Todo POA se vincula a un PEI vigente. | PEI › Generación de POA · RF-005 y T-01 | Intentar generar o activar un POA sin PEI vigente. Comprobar la validación en servidor y la compatibilidad del disparador con los años futuros del PEI. POA C5 aporta el año 2026. |
| RN-002 · p. 9 | Toda actividad tiene responsable. | Actividad › Formulario · RF-008 | Rechazar por API y formulario una actividad sin responsable. El cargo de POA V12:V22 orienta la asignación, que debe resolverse a una identidad o responsabilidad autorizada. |
| RN-005 · p. 9 | Toda evaluación requiere evidencias. | Evaluación › Revisar y emitir · RF-011 y RF-012 | Impedir la emisión de una evaluación sin evidencia aplicable. Vincular un adjunto de prueba a su avance y verificar que la persona evaluadora puede consultarlo. |
| RN-006 · p. 9 | Un avance no puede superar el 100 %. | Avance › Formulario · RF-010, RF-013 y T-01 | Aceptar 100 % y rechazar más de 100 % en el campo regulado. Registrar la política de cantidades ejecutadas superiores a lo planificado y su efecto en cumplimiento. |
| RN-007 · p. 9 | Una actividad vencida genera alerta automática. | Actividad y Centro de alertas · RF-008 y RF-018 | Forzar una fecha pasada en un escenario controlado, ejecutar el proceso de alertas y comprobar que se genera el aviso asociado. Definir el tratamiento de actividades completadas y avisos repetidos. |
| RN-008 · p. 9 | Cumplimiento inferior a 70 % obliga a un plan correctivo. | Evaluación › Plan correctivo · RF-015 | Verificar la obligación al 69 % y su ausencia por umbral al 70 %, también cuando se intenta finalizar mediante API. |
| RN-0098 · p. 9 | Impedir eliminar registros aprobados. | Acciones de registros e historial · T-01 y T-05 | Intentar eliminar un registro aprobado con un usuario que sí puede eliminar borradores. Verificar rechazo y conservación del registro, su versión y sus vínculos. |
| RNF-001 · pp. 9–11 | OAuth2, OpenID Connect y MFA. | Inicio de sesión y Accesos · T-04 con T-01 | Verificar autenticación, sesión y autorización de los seis roles, incluido rechazo de accesos fuera del alcance autorizado. Demostrar MFA con el proveedor configurado y registrar la aclaración de su aparición en RNF-001 y A+. |
| RNF-002 · p. 9 | Auditar usuario, fecha, cambio y valores anterior y nuevo. | Auditoría › Historial y detalle · T-05 | Editar un registro con sesión identificada, recuperar la entrada con los cinco elementos y verificar permisos de consulta. Probar el cambio de estado aprobado, la versión y un cambio de vínculo estratégico. |

Se conserva `RN-0098` tal como aparece en el PDF. La numeración publicada omite RN-003 y RN-004. Los tres casos de uso oficiales de p. 11 se comprueban en el recorrido de T-08.

| Caso oficial | Tareas y verificación en el flujo |
|---|---|
| CU-001 · Crear PEI | RF-001 a RF-004 y T-01. El planificador formula el PEI y se comprueban las transiciones de aprobación y publicación con los actores autorizados. |
| CU-002 · Registrar avance | RF-008 a RF-011. La persona responsable selecciona una actividad, guarda avance y evidencia y vuelve a consultar el registro. |
| CU-003 · Evaluar cumplimiento | RF-011 a RF-015. La persona evaluadora revisa evidencia, valida avance, califica y emite el resultado con sus reglas de cumplimiento. |

## Alcance del SRS descrito fuera de los RF numerados

| Fuente | Pantalla y tarea | Cobertura y criterio propuesto |
|---|---|---|
| Planificación estratégica, pp. 1–4 | PEI › Mapa estratégico y metas plurianuales · RF-001 a RF-004, T-02 y T-03 | Navegar eje, objetivo, resultado, indicador y línea de acción. Consultar metas plurianuales por año y explicar la relación entre la medición estratégica y los productos operativos. |
| Presupuesto y seguimiento, pp. 1–2, 5, 7–10 | POA › Presupuesto y ejecución · RF-009, RF-010 y RF-017 con T-01 y T-03 | Comparar plan y ejecución por período con moneda y versión. Conservar vacío como pendiente. La nota de POA U11 exige asociación con solicitud de insumos y excluye RR. HH., flota adherida a la posición y aumentos salariales en esa columna. |
| Gestión de proyectos, p. 2 | Proyectos › Portafolios, programas, proyectos y cronograma · T-06 | Relacionar un proyecto con la planificación, registrar hitos y entregables y consultar su estado. Acordar relaciones, permisos y detalle de los flujos con T-01 y T-03. |
| Riesgos estratégicos y operativos, p. 2 | Riesgos › Registro, matriz y mitigación · T-07 | Vincular riesgos a la planificación, calcular su clasificación según la matriz de probabilidad e impacto acordada y consultar acciones de mitigación. Las escalas y los umbrales están pendientes. |
| Gestión documental y versionamiento, p. 2 | Evidencias › Historial · RF-011 y T-05 | Conservar el adjunto y el historial de versiones al sustituirlo, con permisos y estados acordados. |
| Seis roles funcionales y módulos, pp. 3–4 y 10 | Administración, Accesos y navegación · T-01, T-02, T-04 y T-08 | Preparar una matriz de permisos para administrador, planificador, responsable de área, evaluador, consultor y consulta ejecutiva. Validar acciones permitidas y denegadas por rol y dependencia. |
| Indicadores clave, pp. 9–10 | Dashboard y Reportes · RF-016, RF-017 y T-03 | Contrastar porcentajes de PEI, eje, objetivo, indicador, POA, ejecución presupuestaria, actividades completadas y productos entregados. Documentar numeradores, denominadores, unidad y reglas de consolidación. |
| Arquitectura recomendada y A+, pp. 10–11 | Decisión técnica y pruebas · T-01, T-03, T-04 y T-08 | Registrar decisiones sobre tecnologías y formatos. Conservar el seguimiento de MFA, correos y aprobación documental de A+ y resolver el tratamiento de MFA con RNF-001. |

## Entradas de referencia y prueba de trazabilidad

| Fuente | Dato utilizable | Control de calidad |
|---|---|---|
| Ejes A4:E23 | Seis ejes y veinte objetivos, cada uno con resultado esperado, indicador y línea de acción. | Propagar el encabezado de eje por sus celdas combinadas. Conservar nombres, datos de origen y códigos internos únicos. |
| POA B12:V22 | Once registros de productos de TI 2026, una unidad porcentual y diez en unidades, con metas anuales y mensuales. | Mantener identificadores independientes del rótulo del producto. Verificar que las sumas mensuales reproducen H12:H22. |
| POA D12:D22 y F12:F22 | Dos celdas con actividad explícita y once descripciones de medios de verificación. | Crear actividades adicionales solo si se identifican como datos de demostración o si una fuente las confirma. El texto de verificación describe el documento esperado. |
| POA U12:U22 y V12:V22 | Presupuesto DOP en blanco y encargado de seguimiento expresado como cargo. | Mostrar presupuesto pendiente, asignar responsable autorizado y preparar avances, adjuntos y riesgos de prueba. |

**Recorrido de demostración propuesto.** Vincular el eje «Fortalecimiento y desarrollo organizacional» con el objetivo de capacitación de personal y el indicador «Porcentaje de personal capacitado» de `Ejes A15:E15`. Asociar el producto de socialización de herramientas TIC de `POA C14` a ese objetivo tras revisar la alineación. Programar una jornada en abril, mayo y junio según `POA L14:N14`. Registrar una actividad y su lista de asistencia de prueba según `POA F14`. En el primer cuatrimestre, 0 jornadas ejecutadas de 1 programada producen 0 % y obligan a un plan correctivo, siempre que el caso disponga de evidencia válida y evaluable. En una evaluación de prueba independiente, 1 jornada ejecutada de 1 programada produce 100 %. La cantidad de jornadas es la medida operativa. El porcentaje de personal capacitado requiere además conocer cuántas personas se capacitaron y cuál es la población de referencia aprobada para el indicador.

## Ampliación solicitada de actividades no planificadas

| Tarea del portal | Pantalla propuesta | Aceptación propuesta |
|---|---|---|
| NP-01 · Registro | POA › Actividades no planificadas › Nueva | Guardar una solicitud con origen, motivo, dependencia, fechas y datos pendientes. Conservar el historial y comprobar que la solicitud deja intacta la programación aprobada. |
| NP-02 · Revisión e incorporación | Solicitud › Revisión y cambio de versión | Registrar decisión con actor, fecha y motivo. Incorporar una actividad autorizada mediante revisión identificable y conservar la línea base y el origen no planificado. |
| NP-03 · Comparación | Reportes › Línea base, revisión y ejecución | Mostrar las tres vistas de datos y conciliar cantidades y costos sin contabilizar dos veces una actividad incorporada. |

Los criterios NP requieren definir autorización, vigencia, impacto en cálculo y presupuesto antes de cerrar aceptación. Los IDs `PROP-` de la ampliación conservan su propia numeración y fuente. [Alcance de la ampliación en el repositorio](https://github.com/meliodr/Reto-desarollo-web-/blob/b1d7586d7e87e6544fc40e600551b7ba5fe6751a/docs/AMPLIACION-NO-PLANIFICADA.md)

## Decisiones y evidencia para cerrar la matriz

T-01 y T-03 deben resolver el disparador de generación, alcance de roles y transiciones, denominador cero, redondeo, sobreejecución, rangos decimales del semáforo, agregación por unidad y período, consolidación institucional, detalle presupuestario, versionado, MFA y notificaciones. Cada definición pendiente afecta los casos señalados en su fila. [Decisiones y asuntos pendientes del proyecto](https://github.com/meliodr/Reto-desarollo-web-/blob/b1d7586d7e87e6544fc40e600551b7ba5fe6751a/docs/DECISIONES.md)

T-08 registrará por criterio el ID, versión o commit probado, entorno, rol, datos, pasos, resultado esperado y observado, evidencia y revisor. El tablero documenta 29 tareas de preparación y desarrollo. Sus pruebas verifican el funcionamiento del portal de trabajo. La aceptación de SISPLAN requiere ejecutar los casos contra el producto institucional. La captura del aula proporciona el plazo visible. La rúbrica y los formatos exigidos para la entrega siguen sin confirmarse en las fuentes disponibles. [Método del tablero y revisión](https://github.com/meliodr/Reto-desarollo-web-/blob/b1d7586d7e87e6544fc40e600551b7ba5fe6751a/docs/PORTAL.md)

## Pruebas del incremento de planificación estratégica

| Requisito | Resultado local del 13 de noviembre | Evidencia |
|---|---|---|
| RF-001 | Parcial. Crear, editar y recuperar el borrador. Aprobación y publicación pendientes. | `tests/pei_smoke.py` y recorrido Chromium |
| RF-002 | Verificado en la rama del PR. Alta, edición, inactivación, historial persistente y conflicto entre ediciones. | `tests/ejes_smoke.py` y recorrido Chromium |
| RF-003 | Verificado en la rama del PR. Los cinco campos se recuperan al volver a abrir el eje, incluso después de inactivarlo. | `tests/objetivos_smoke.py` y recorrido Chromium |

La verificación se hizo con Angular, API .NET y PostgreSQL 18 de prueba. La revisión del equipo y la integración siguen pendientes. [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) · [Instrucciones de prueba](06-PRIMER-INCREMENTO.md)

## Cobertura de publicación del PEI y generación del POA

| Requisito | Resultado del 13 de noviembre en la rama del PR | Evidencia reproducible |
|---|---|---|
| RF-001 | Verificado. Crear el PEI, recuperar sus datos, enviar, devolver, aprobar con otro planificador y publicar. Actualiza el resultado parcial anterior. | `tests/pei_smoke.py`, `tests/flujo_smoke.py` y recorrido Chromium |
| RF-004 | Verificado. Registrar los seis campos del indicador, guardar una medición por fecha y consultar su definición y valor. | `tests/indicadores_smoke.py`, pruebas Angular y recorrido Chromium |
| RF-005 | Verificado. Generar cuatro POA para un PEI ordinario y cinco para uno extraordinario. Repetir publicación conserva los mismos registros. | `tests/flujo_smoke.py` y recorrido Chromium |
| RN-001 | Verificado en la generación. Los POA nacen al publicar un PEI, quedan vinculados a esa versión y respetan su intervalo. Se comprueban POA vigentes y futuros, y el rechazo de publicaciones solapadas. | `tests/flujo_smoke.py` |
| RF-012 | Parcial. Se generan tres evaluaciones por POA con meses oficiales y sin duplicados. Falta probar los avances por corte y la emisión con evidencia. | `tests/flujo_smoke.py` y vista de POA |
| RNF-001 | Parcial. Se verificó OIDC con Keycloak, el rol Planificador y una cuenta sin permiso. Falta MFA y la verificación de los seis roles y su alcance. | Inicio de sesión en Chromium y `tests/flujo_smoke.py` |

Con RF-002 y RF-003, la rama reúne seis de los 27 requisitos y reglas numerados con cobertura verificada (22,2 %). La integración en `main` y la revisión de otro integrante siguen pendientes. [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) · [Pasos de verificación](06-PRIMER-INCREMENTO.md)
