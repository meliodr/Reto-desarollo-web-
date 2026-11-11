# Estado del proyecto

## Situación actual

| Componente | Estado | Detalle |
|---|---|---|
| Consigna y SRS | Analizados | 11 páginas revisadas; 18 RF, 7 RN, 2 RNF y 3 casos de uso oficiales |
| Catálogo DIGEMAPS | Analizado | 6 ejes y 20 objetivos con resultados, indicadores y líneas de acción |
| POA TI 2026 | Analizado | 11 registros de productos con metas mensuales y datos pendientes |
| Organización del equipo | Cinco plazas configuradas | Incorporación y recorridos con las cinco cuentas reales pendientes |
| Portal de trabajo v2 | Publicado y revisado | [Portal público](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/) con tareas compartidas, checklist, notas, encuestas y revisión cruzada |
| Repositorio | Documentación de preparación | README, método, estado, decisiones, guía del portal, identidad, ampliación y referencia del reto anterior |
| Nombre e identidad visual | En exploración | SISPLAN es el nombre empleado por el SRS; no se ha seleccionado un nombre final |
| Arquitectura | Pendiente | Tecnologías y estructura de la aplicación por seleccionar |
| Actividades no planificadas | Ampliación solicitada | Alcance y reglas por definir; se mantiene separada del SRS original |
| Implementación del sistema institucional | **SIN INICIAR** | No hay código funcional del producto en este repositorio |
| Pruebas del sistema institucional | **NO EJECUTADAS** | Las pruebas del portal se registran por separado de las del producto |
| Despliegue del sistema institucional | Pendiente | El portal publicado contiene documentación del proyecto |
| Cumplimiento global del SRS | Sin calcular | Todavía no hay implementación que permita medirlo |

## Publicación y verificación del portal v2

La publicación fue confirmada y se revisó el sitio público en navegador. La lectura anónima carga el estado compartido real, con 29 tareas y cinco plazas disponibles durante esta revisión. El enlace de inicio de sesión está visible. No se crearon miembros ficticios en la base pública.

La versión publicada del portal corresponde al commit fuente `b099822bd8d48984fd58229a9cbc1481d97e4e43`. Esa referencia identifica el portal; este repositorio conserva la documentación para preparar el producto académico.

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

## Reglas pendientes de precisar

| Tema | Definición pendiente |
|---|---|
| Cumplimiento | Meta programada en cero, redondeos, valores negativos y ejecución superior a la meta |
| Semáforo | Tratamiento de decimales entre 89 % y 90 % |
| Consolidación | Fórmula de cumplimiento por institución, eje, área, objetivo e indicador |
| Aprobaciones | Permisos y estados para formular, revisar, aprobar, publicar y devolver |
| Versionado | Cambios en registros aprobados y reemplazo de evidencias |
| Organización del POA | Consolidación institucional y distribución por dependencia |
| Presupuesto | Detalle, moneda, ejecución y distribución entre productos y actividades |
| Proyectos y riesgos | Desarrollo del alcance mencionado en el SRS |
| MFA | Su aparición como requisito no funcional y como elemento «A+» |
| Actividades no planificadas | Registro, autorización, seguimiento y relación con metas y presupuesto |

## Calendario de referencia

La consigna disponible indica apertura el **1 de diciembre de 2026 a las 00:00** y cierre el **2 de diciembre de 2026 a las 23:59**.

El objetivo referido a la **semana 6** está pendiente de conciliar con ese plazo. Por ahora no se fija una fecha distinta de la visible en el aula.

## Trabajo siguiente

1. Completar criterios de aceptación y resolver las dudas del SRS.
2. Definir el alcance de las actividades no planificadas.
3. Preparar el modelo de datos, los estados y los permisos.
4. Seleccionar la arquitectura y distribuir tareas entre los cinco integrantes.
5. Planificar pruebas y demostración.

Estos puntos organizan el desarrollo del equipo. La relación entre requisitos originales y ampliaciones se mantiene en [DECISIONES.md](DECISIONES.md).
