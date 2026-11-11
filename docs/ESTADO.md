# Estado del proyecto — SISPLAN

Corte de esta preparación: **10 de noviembre de 2026, hora de República Dominicana (UTC−4)**.

Este documento diferencia el trabajo de análisis, la preparación del proyecto y la implementación del sistema. Sus estados deben actualizarse al incorporar evidencia nueva.

## Situación acreditada

| Elemento | Estado | Evidencia o límite |
|---|---|---|
| Consigna del reto | Analizada | Define una plataforma institucional PEI-POA y trazabilidad hasta actividades |
| SRS SISPLAN | Analizado y catalogado en el portal | 11 páginas revisadas, incluidos diagramas; 18 RF, 7 RN, 2 RNF y 3 casos de uso oficiales |
| Catálogo DIGEMAPS | Analizado | 6 ejes y 20 objetivos con resultados, indicadores y líneas de acción |
| POA TI 2026 | Analizado | 11 registros de productos con programación mensual; faltantes documentados |
| Referencia de FuelTrack | Revisada | Se estudiaron documentación, metodología, revisiones, pruebas e integración |
| Equipo | Preparación | Equipo de cuatro integrantes; nombres y asignaciones no consignados aquí |
| Repositorio | Inicializado | README, método de trabajo y este registro |
| Portal del proyecto v1 | **PUBLICADO Y VERIFICADO** | [Portal público](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/); requisitos, casos de uso, documentos descargables, calendario y equipo de cuatro integrantes |
| Arquitectura definitiva | Pendiente | No se ha fijado un stack |
| Implementación de la aplicación | **SIN INICIAR** | No hay código funcional de SISPLAN en este repositorio |
| Pruebas del producto SISPLAN | **NO EJECUTADAS** | La verificación del portal de seguimiento y las pruebas históricas de FuelTrack no son pruebas del producto institucional |
| Despliegue del sistema institucional | No acreditado | El portal del proyecto es un recurso de seguimiento, no prueba de una aplicación institucional implementada |
| Cumplimiento global del SRS | No calculado | No hay base para publicar un porcentaje de funcionalidad construida |

## Portal público v1 y verificación

El [portal del proyecto](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/) está publicado. Reúne **18 requisitos funcionales, 7 reglas de negocio, 2 requisitos no funcionales y 3 casos de uso oficiales**. Presenta además **9 casos de uso propuestos**, identificados por separado para que no se atribuyan al SRS del docente.

Incluye documentos descargables, calendario y organización para un equipo de cuatro integrantes.

En la revisión del portal v1 se comprobaron:

| Comprobación del portal | Resultado observado |
|---|---|
| Navegación entre vistas | Funcional |
| Filtros de requisitos y casos de uso | Funcionales |
| Detalles y referencias a la fuente | Consultables |
| Simulador de cumplimiento | Revisado en el límite del 70 % y con meta programada igual a cero |
| Descargas | CSV, Excel y SRS comprobados |
| Vistas estrechas | Revisadas a 375 y 305 píxeles, sin desbordamiento horizontal general |

Estas comprobaciones corresponden **exclusivamente al portal de preparación y seguimiento**. No acreditan implementación, pruebas, despliegue ni cumplimiento funcional del producto institucional SISPLAN. El simulador es un recurso explicativo del portal; no sustituye la definición y validación de las reglas del producto.

## Hallazgos de las fuentes

- El catálogo suministrado tiene seis ejes. Los cinco ejes enumerados en el SRS son ejemplos y no deben reemplazar automáticamente el catálogo real.
- Los productos del POA no identifican explícitamente su objetivo e indicador estratégico; esos vínculos deben completarse sin inventarlos.
- La plantilla del POA agrupa los meses en cuatro trimestres. El sistema debe generar las tres evaluaciones cuatrimestrales exigidas por el SRS.
- El presupuesto de los once productos está vacío. Vacío no significa cero.
- Solo dos productos tienen actividades detalladas en la columna correspondiente.
- Hay dos registros rotulados «Producto 8». Los identificadores internos no pueden depender de ese rótulo.
- Los medios de verificación del Excel describen evidencias esperadas; no acreditan que las actividades estén ejecutadas.
- Las metas usan cantidades y porcentajes. Cada indicador requiere unidad y regla de cálculo propias.

## Decisiones o aclaraciones pendientes

| Tema | Qué falta definir |
|---|---|
| Cálculo de cumplimiento | Programado igual a cero, redondeos, valores negativos y ejecución superior a la meta |
| Semaforización | Formalizar amarillo como desde 70 % hasta menos de 90 % para incluir decimales |
| Consolidación | Fórmula de cumplimiento por institución, eje, área, objetivo e indicador |
| Aprobaciones | Matriz de permisos y estados para formular, revisar, aprobar, publicar y devolver |
| Versionado | Tratamiento de cambios en registros aprobados y evidencias reemplazadas |
| POA institucional y áreas | Precisar la organización del POA anual y su consolidación por dependencia |
| Presupuesto | Nivel de detalle, ejecución, moneda y distribución entre productos y actividades |
| Proyectos y riesgos | Profundidad funcional del alcance declarado en el SRS |
| MFA | Resolver su aparición simultánea como requisito no funcional y elemento «A+» |
| Arquitectura | Selección técnica y justificación documentada, sin convertir opciones recomendadas en obligaciones no confirmadas |

## Próximos entregables de preparación

1. Completar la matriz de requisitos del portal con criterios de aceptación y resolución de dudas.
2. Modelo de datos y estados del proceso.
3. Matriz de permisos de los seis roles funcionales.
4. Decisión de arquitectura y plan de implementación por flujos.
5. Reparto de tareas entre los cuatro integrantes.
6. Plan de verificación y guion de demostración.

Estos puntos son **propuestas de organización del equipo**, no entregables adicionales atribuidos automáticamente al docente.

## Regla de actualización

Actualizar este registro con enlaces a los cambios y evidencias correspondientes. No sustituir «pendiente» por «completado» solo porque existe una pantalla, un documento, una rama o un número de pruebas. Conservar fechas y commits cuando se cite una revisión histórica.
