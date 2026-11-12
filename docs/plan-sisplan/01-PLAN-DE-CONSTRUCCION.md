# Plan base de SISPLAN

**Versión 1.0 · 12 de noviembre de 2026 · Equipo de cinco**

La aplicación usará el nombre **SISPLAN** para la entrega. SISPLAN gestiona el ciclo institucional completo desde el PEI hasta la evaluación del POA. El primer recorrido integrado debe publicar un PEI, generar los POA, vincular una actividad con su objetivo, registrar un avance con evidencia y publicar una evaluación. Ese recorrido fija el contrato que comparten los cinco frentes.

| Documento vigente | Contenido |
|---|---|
| [Reglas operativas](07-REGLAS-OPERATIVAS.md) | Roles, vigencia, estados, cálculos, reportes, presupuesto y actividades no planificadas |
| [Herramientas y entornos](08-HERRAMIENTAS-Y-ENTORNOS.md) | Stack, autenticación, archivos, exportaciones, pruebas, CI y demo |
| [Matriz de trazabilidad](05-TRAZABILIDAD.md) | Fuente, pantalla, tarea y criterio de aceptación de cada requisito |

Las decisiones de estos tres documentos rigen la implementación. [Flujos iniciales](02-FLUJOS.md), [borrador de datos/API/QA](03-DATOS-API-QA.md) y [diseño inicial Angular](04-FRONTEND-ANGULAR.md) son referencias de diseño. Si una propuesta previa difiere de la base 1.0, se aplica la base 1.0. El SRS sigue siendo la fuente del alcance. Angular fue elegido por el grupo. Las demás decisiones son la base de trabajo de este plan, para que el grupo pueda revisarlas como un conjunto.

## Alcance acordado para construir

| Bloque | Resultado exigible | Trazabilidad |
|---|---|---|
| Acceso y administración | Seis roles, dependencias, catálogos, usuarios, OAuth2/OIDC con MFA y auditoría de cambios | RNF-001, RNF-002, T-01, T-04, T-05 |
| Estrategia | PEI ordinario y extraordinario, ejes, objetivos, resultados, líneas de acción, indicadores, metas plurianuales y mapa | RF-001 a RF-004, CU-001 |
| Operación | Un POA por año, productos vinculados, actividades con responsable, programación anual/mensual/cuatrimestral y presupuesto | RF-005 a RF-009, RN-001, RN-002 |
| Seguimiento | Avance físico y financiero, comentarios y evidencias versionadas, evaluación de los tres cuatrimestres, semáforo y correctivos | RF-010 a RF-015, CU-002, CU-003, RN-005, RN-006, RN-008 |
| Resultados | Dashboard, seis informes en PDF y XLSX y cuatro tipos de alertas internas | RF-016 a RF-018, RN-007 |
| Proyectos y riesgos | Portafolios, programas, proyectos, hitos, entregables y matriz de riesgo con mitigación | Alcance del SRS §1.3, T-06 y T-07 |
| No planificadas | Solicitud, decisión, registro, evaluación e incorporación por revisión prospectiva con trazabilidad | Ampliación del equipo, NP-01 a NP-03 |

El alcance base contiene los **18 RF, 7 RN y 2 RNF**, conserva `RN-0098` y los tres casos de uso oficiales. El correo y la aprobación de documentos como flujo independiente quedan para una ampliación posterior a la entrega. El MFA va en el acceso base por RNF-001. Los Excel del reto se usan como referencia y datos de demostración curados. La importación masiva automática queda fuera de esta entrega.

## Reparto y esfuerzo de referencia

| Frente | Responsable de entregar | Primera interfaz compartida | Revisión cruzada | Esfuerzo estimado |
|---|---|---|---|---|
| 1. Base y acceso | Infraestructura, CI, Keycloak/MFA, usuarios, permisos, catálogos, alertas | `/me`, roles y dependencias | 4 | 24 h |
| 2. Estrategia y experiencia | Shell Angular, PEI, ejes, objetivos, indicadores, mapa y generación de POA | Contrato PEI publicado y POA generado | 3 | 28 h |
| 3. Operación y proyectos | Productos, actividades, programación, portafolios, programas y proyectos | Producto y actividad aprobados con metas por período | 5 | 27 h |
| 4. Seguimiento y evaluación | Avances, evidencias, validación, evaluación, correctivos y auditoría | Resultado publicado con trazabilidad | 2 | 30 h |
| 5. Presupuesto y resultados | Presupuesto, dashboard, exportaciones, riesgos y ampliación no planificada | Modelo de consulta y exportación por corte | 1 | 30 h |

**Estimación inicial: 139 horas de equipo**, incluidas integración y revisión. Es un supuesto de planificación para cinco personas durante unas dos semanas y media, aproximadamente 11 horas semanales por integrante. Registrar horas reales y nombres en el tablero al iniciar. Los responsables de frente entregan pantalla, API, datos, contrato y verificación con apoyo de sus revisores. Todos participan en la integración. El frente 1 coordina el ensayo y el frente 4 mantiene el registro de resultados de aceptación.

## Fases y condiciones para avanzar

| Fecha interna | Entrega verificable | Condición de corte |
|---|---|---|
| 12–14 nov | Plan revisado, responsables asignados, entorno reproducible, flujo de acceso con MFA y contrato PEI/POA | Otro integrante instala en limpio y abre una pantalla autenticada con alcance correcto. El PR #1 se revisa contra esta base antes de integrar |
| 15–19 nov | PEI con estructura publicado, POA por año, tres evaluaciones por POA, producto y actividad programados | Una ejecución reproducible demuestra vínculos y permisos. Repetir la generación no duplica registros. Revisar horas restantes y reasignar apoyo si hay desvíos |
| 20–24 nov | Avance físico y financiero con evidencia, evaluación validada, emisión y publicación, correctivo rojo | Probar con cuentas distintas y verificar rechazo de evaluación sin evidencia, acceso fuera de dependencia y edición con versión obsoleta |
| 25–28 nov | Dashboard, seis reportes en dos formatos, cuatro alertas, proyectos, riesgos y trabajo no planificado | Las cifras cuadran para el mismo corte. El flujo adicional conserva línea base y evita duplicar gastos. Marcar cobertura real en la matriz |
| 29–30 nov | Cierre de funciones, instalación en limpio, ensayo y versión etiquetada | Arrancar en otro equipo, recorrer el guion con datos ficticios, guardar evidencia por requisito y corregir fallos que impidan la demo |
| 1–2 dic | Preparar y entregar el paquete en el aula | Revisar rúbrica, formato, enlaces y cierre que muestre el aula. Registrar la entrega y el commit exacto |

La captura disponible muestra apertura el **1 de diciembre a las 00:00** y cierre el **2 de diciembre a las 23:59**. El objetivo interno es llegar al **30 de noviembre** con la demo reproducible. Se conserva la fecha visible del aula hasta que el maestro confirme cómo se relaciona con la semana 6. La zona horaria de la captura, la rúbrica completa y el formato exacto de subida se verifican en el aula. La implementación puede usar la base acordada mientras se obtiene esa información.

En paralelo al recorrido principal, preparar plantillas de informes y casos de alertas, proyectos y riesgos con datos ficticios. Si el 19 de noviembre el recorrido sigue bloqueado, el equipo reasigna tiempo al contrato o módulo responsable y simplifica presentación y automatizaciones secundarias. Si queda un requisito del SRS en riesgo, se registra y se consulta al maestro antes de recortarlo.

## Cómo se inicia y se cierra una tarea

1. Tomar la tarea en el tablero y registrar requisito, responsable, revisor, dependencia y escenario esperado. Acordar el DTO y sus errores en OpenAPI antes de construir en paralelo.
2. Trabajar en una rama corta. Entregar PR con instrucciones de prueba, caso válido y caso de rechazo relevantes. Los cambios de esquema llevan migración. Los cambios de regla ajustan su prueba y su trazabilidad.
3. Revisión por otro integrante, compilaciones y pruebas aplicables aprobadas y caso funcional reproducido. Para requisitos de permisos, probar también el caso denegado. Una migración se prueba sobre base vacía y base del incremento anterior.
4. Guardar el SHA, resultado y evidencia en la tarea. La matriz mantiene `pendiente`, `en curso`, `verificado` o `bloqueado`. Solo `verificado` cuenta como cobertura probada.

En la primera sesión, dedicar 15 minutos a nombres y horas, 20 a revisar la base 1.0 y la consigna, 25 a revisar un ejemplo del contrato PEI/POA y 30 a preparar el entorno y probar el acceso. Los desacuerdos se anotan como cambios concretos a una decisión, con responsable y prueba afectada.

## Guion y paquete de entrega

1. Con acceso MFA, formular un PEI y publicarlo con el rol autorizado. Mostrar sus POA, cuatrimestres y trazabilidad estratégica.
2. Programar tres unidades de una actividad y registrar dos ejecutadas con evidencia. Con otra cuenta validar y emitir. Mostrar el resultado rojo de 66,66 %, el correctivo y el registro de auditoría. Publicar y comparar dashboard, PDF y XLSX del mismo corte.
3. Mostrar alertas, un proyecto con hito y entregable, un riesgo con mitigación y una actividad adicional incorporada sin alterar la línea base. Reproducir un permiso denegado, un caso sin evidencia y un conflicto de versión.
4. Entregar repositorio y versión etiquetada, instrucciones verificadas, configuración de ejemplo, migraciones, datos ficticios, diagramas, contrato API, matriz, guion y resultados de prueba. La rúbrica del aula determina qué archivos se suben.

El [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) conserva un prototipo local de borradores PEI, con builds e integración local verificados. Se revisará contra este plan antes de incorporarlo a la rama principal. La cobertura oficial se medirá sobre la versión integrada.

[Consigna y documentos del reto](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/#documentos) · [Tablero del equipo](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/#tablero)
