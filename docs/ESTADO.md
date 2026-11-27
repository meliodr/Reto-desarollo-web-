# Estado del proyecto

La [adaptación al SRS v2](plan-sisplan/10-ADAPTACION-SRS-V2.md) recoge el alcance vigente. El [plan por fases](plan-sisplan/11-PLAN-POR-FASES-SRS-V2.md) organiza el trabajo, las dependencias y los criterios de aceptación. La información sobre el portal documenta su publicación anterior.

## Situación actual

| Componente | Estado | Detalle |
|---|---|---|
| Consigna y SRS | Analizados | SRS v2 comparado con el SRS original y las dos hojas de cálculo |
| Catálogo DIGEMAPS | Cotejado | 20 filas del Excel coinciden con los 6 ejes, objetivos, indicadores, resultados y líneas de acción del catálogo |
| POA TI 2026 | Analizado | 11 registros de productos con metas mensuales y datos pendientes |
| Organización del equipo | Cinco plazas configuradas | Incorporación y recorridos con las cinco cuentas reales pendientes |
| Portal de trabajo v2 | Publicado y revisado | [Portal público](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/) con tareas compartidas, checklist, notas, encuestas y revisión cruzada |
| Base institucional F0 | Implementada en el [PR #2](https://github.com/meliodr/Reto-desarollo-web-/pull/2) | Sesión por institución, roles y dependencias, módulos autónomos, RLS, datos históricos y auditoría. [Matriz, contratos y decisiones](plan-sisplan/14-CONTRATO-F0.md). |
| Formulación y publicación PEI F1 | Implementada en la rama de F1 | [Criterios y verificación de F1](plan-sisplan/15-CIERRE-F1.md) |
| Repositorio | Plan vigente para el SRS v2 | [Fases, subfases y criterios de aceptación](plan-sisplan/11-PLAN-POR-FASES-SRS-V2.md) |
| Nombre de la aplicación | SISPLAN | Nombre de trabajo para la entrega académica |
| Arquitectura | Definida en la base | Angular 22, .NET 10 LTS, PostgreSQL 18, Npgsql y Keycloak con MFA. Demo local reproducible |
| Actividades no planificadas | Regla de trabajo definida | [Registro, autorización e incorporación](plan-sisplan/07-REGLAS-OPERATIVAS.md#trabajo-no-planificado-proyectos-riesgos-y-alertas) |
| Implementación del sistema institucional | Prototipo en PR | [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) cubre carga DIGEMAPS, revisión y publicación del PEI, y creación de POA independiente o vinculado con presupuesto de referencia opcional |
| Pruebas del sistema institucional | Por revisión del PR | La [verificación de referencia](https://github.com/meliodr/Reto-desarollo-web-/actions/runs/45904335830) aprobó 26 pruebas Angular, cinco regresiones HTTP, migraciones, permisos institucionales y el recorrido en Chromium. Los [controles del commit actual](https://github.com/meliodr/Reto-desarollo-web-/pull/2/checks) determinan el resultado de F0. |
| Despliegue del sistema institucional | Pendiente | El portal publicado contiene documentación del proyecto |
| Cumplimiento global del SRS | Sin calcular | La versión integrada y sus criterios de aceptación siguen pendientes |

## Publicación y verificación del portal v2

La publicación fue confirmada y se revisó el sitio público en navegador. La lectura anónima carga el estado compartido real, con 29 tareas y cinco plazas disponibles durante esta revisión. El enlace de inicio de sesión está visible. No se crearon miembros ficticios en la base pública.

La verificación corresponde a la versión 2 del portal. Este repositorio conserva la documentación para preparar el producto académico; el código y la publicación del portal se gestionan por separado.

| Verificación del portal | Resultado |
|---|---|
| Servicio del tablero | 17 pruebas con SQLite aprobadas |
| Regresiones de interfaz | 4 pruebas JavaScript aisladas aprobadas |
| Total automatizado | **21 aprobadas, 0 fallidas y 0 omitidas** |
| TypeScript | Sin errores |
| Navegador local | Lectura, filtros, detalle de tarea y checklist anónimo, prototipo y adaptación a 320 píxeles comprobados |
| Navegador público | Carga del snapshot real, lectura anónima, cinco plazas y enlace de inicio de sesión comprobados |
| Cinco cuentas reales | Pendiente de incorporación y recorrido de cada integrante |

Las pruebas automatizadas se ejecutaron en el entorno de desarrollo del portal. No equivalen a una validación completa con las cinco cuentas ni a pruebas del sistema institucional. La guía de participación está en [PORTAL.md](PORTAL.md).

## Revisión anterior del portal v1

La revisión del **10 de noviembre de 2026, hora de República Dominicana**, registró estas comprobaciones:

| Comprobación | Resultado |
|---|---|
| Navegación | Las vistas se abren |
| Filtros de requisitos y casos de uso | Funcionan |
| Detalles de requisitos y referencias | Se pueden consultar |
| Simulador de cumplimiento | Revisado en el límite de 70 % y con meta programada igual a cero |
| Descargas | CSV, Excel y SRS comprobados |
| Vistas estrechas | Revisadas a 375 y 305 píxeles, sin desbordamiento horizontal general |

El portal reúne los requisitos del SRS y presenta **9 casos de uso propuestos**, identificados aparte de los **3 oficiales**. El simulador explica reglas de cumplimiento; su revisión no equivale a haber desarrollado el cálculo del sistema institucional.

## Hallazgos del análisis

- El catálogo tiene seis ejes. Los cinco nombres enumerados en el SRS son ejemplos.
- Los productos del POA no identifican su objetivo e indicador estratégico.
- La plantilla agrupa los meses en cuatro trimestres. Para el seguimiento se aplica la periodicidad del SRS v2 conforme se implemente.
- Los presupuestos de los once productos están vacíos. Según la aclaración del maestro, se permite registrar un valor de referencia o dejar el campo vacío.
- Solo dos productos tienen actividades detalladas en la columna correspondiente.
- Dos registros llevan el rótulo «Producto 8».
- La columna de verificación describe las evidencias esperadas, sin acreditar ejecución.
- Las metas incluyen cantidades y porcentajes que requieren reglas de cálculo diferentes.

## Reglas de trabajo

El flujo actual y el presupuesto opcional se rigen por la [adaptación al SRS v2](plan-sisplan/10-ADAPTACION-SRS-V2.md). Las [reglas de la base v1.0](plan-sisplan/07-REGLAS-OPERATIVAS.md) sirven de antecedente.

## Calendario de referencia

Estamos en la semana 4. El objetivo es entregar en la semana 7 con el candidato validado al cierre de la semana 6. La [actividad vigente](https://campusvirtual.intec.edu.do/mod/assign/view.php?id=1260918), consultada el 26 de noviembre, cierra el 2 de diciembre de 2026 a las 23:59. El equipo debe ajustar el corte de envío a ese plazo. [Calendario de trabajo y capacidad del equipo](plan-sisplan/12-ENTREGA-SEMANA-7.md)

## Trabajo siguiente

1. Confirmar responsables, revisores y horas del equipo al cerrar la semana 4.
2. Aplicar en F2 la política de [autenticación, validación del Comité, aprobación y publicación](plan-sisplan/14-CONTRATO-F0.md#decisiones-operativas-para-el-cierre-de-f0).
3. Usar las cuentas ficticias de F0 para el primer ensayo de PEI y POA.
4. Construir y probar el POA mínimo de la semana 5 con un producto, una actividad, una meta y un responsable.
