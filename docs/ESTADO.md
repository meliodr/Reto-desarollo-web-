# Estado del proyecto

## Situación actual

| Componente | Estado | Detalle |
|---|---|---|
| Consigna y SRS | Analizados | 11 páginas revisadas; 18 RF, 7 RN, 2 RNF y 3 casos de uso oficiales |
| Catálogo DIGEMAPS | Analizado | 6 ejes y 20 objetivos con resultados, indicadores y líneas de acción |
| POA TI 2026 | Analizado | 11 registros de productos con metas mensuales y datos pendientes |
| Organización del equipo | Cinco plazas configuradas | Incorporación y recorridos con las cinco cuentas reales pendientes |
| Portal de trabajo v2 | Publicado y revisado | [Portal público](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/) con tareas compartidas, checklist, notas, encuestas y revisión cruzada |
| Repositorio | Base de trabajo v1.0 | [Plan base](plan-sisplan/01-PLAN-DE-CONSTRUCCION.md) con reglas, herramientas, tareas y criterios de aceptación |
| Nombre de la aplicación | SISPLAN | Nombre de trabajo para la entrega académica |
| Arquitectura | Definida en la base | Angular 22, .NET 10 LTS, PostgreSQL 18, Npgsql y Keycloak con MFA. Demo local reproducible |
| Actividades no planificadas | Regla de trabajo definida | [Registro, autorización e incorporación](plan-sisplan/07-REGLAS-OPERATIVAS.md#trabajo-no-planificado-proyectos-riesgos-y-alertas) |
| Implementación del sistema institucional | Prototipo en PR | [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) cubre borradores de PEI en desarrollo local |
| Pruebas del sistema institucional | Verificación parcial del prototipo | 11 pruebas Angular e integración de borradores con PostgreSQL documentadas en el PR. La aceptación del SRS se mide sobre la entrega integrada |
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
- La plantilla agrupa los meses en cuatro trimestres, mientras que el SRS exige tres evaluaciones cuatrimestrales.
- Los presupuestos de los once productos están vacíos.
- Solo dos productos tienen actividades detalladas en la columna correspondiente.
- Dos registros llevan el rótulo «Producto 8».
- La columna de verificación describe las evidencias esperadas, sin acreditar ejecución.
- Las metas incluyen cantidades y porcentajes que requieren reglas de cálculo diferentes.

## Reglas de trabajo

Vigencia, permisos, evaluación, presupuesto, alertas y trabajo adicional se implementan según las [reglas operativas de la base v1.0](plan-sisplan/07-REGLAS-OPERATIVAS.md). Cualquier aclaración del maestro se registra con el requisito y el caso de prueba afectado.

## Calendario de referencia

La consigna disponible indica apertura el **1 de diciembre de 2026 a las 00:00** y cierre el **2 de diciembre de 2026 a las 23:59**.

El objetivo referido a la **semana 6** está pendiente de conciliar con ese plazo. Por ahora no se fija una fecha distinta de la visible en el aula.

## Trabajo siguiente

1. Registrar en el tablero responsables, revisores y horas disponibles.
2. Revisar la base v1.0 con el grupo y verificar la rúbrica, el formato de entrega y el plazo en el aula.
3. Revisar el PR #1 frente a la base, reproducir el entorno y comprobar acceso con MFA.
4. Ejecutar el primer corte de PEI publicado, POA generado y actividad programada.
