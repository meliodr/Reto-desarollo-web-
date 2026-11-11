# Ampliación: actividades no planificadas

## Origen y estado

Se ha solicitado incluir trabajo no planificado en la futura aplicación. El SRS original no desarrolla un RF específico para este proceso. La necesidad se incorpora al análisis; el flujo, los permisos y las reglas siguientes son propuestas pendientes de acuerdo.

La propuesta reúne **9 requisitos funcionales, 10 reglas de negocio y 5 casos de uso**, identificados con prefijo `PROP-`. No se renumeran los 18 RF, las 7 RN ni los 3 casos de uso originales del SRS.

## Qué se busca registrar

Una actividad no planificada surge fuera de la programación aprobada del POA. Debe conservar su origen aunque posteriormente se incorpore mediante una revisión autorizada.

Los datos propuestos incluyen:

| Grupo | Datos |
|---|---|
| Identificación | Título, descripción, motivo, prioridad, fecha del hecho y fecha de registro |
| Responsabilidad | POA y año, dependencia, solicitante y responsable propuesto |
| Planificación | Producto u objetivo relacionado, clasificación pendiente si corresponde, meta y unidad, cronograma y versión de referencia |
| Finanzas | Costo estimado, autorizado y real diferenciados; moneda y financiación cuando se conozcan |
| Control | Decisiones con autor, motivo y fecha; evidencias, revisión e incorporación al POA |

Un dato desconocido debe mantenerse como pendiente, en lugar de convertirse en cero. Los vínculos estratégicos se registran cuando tienen fundamento; su ausencia inicial no justifica inventarlos.

## Capacidades propuestas

| ID | Capacidad |
|---|---|
| PROP-RF-NP-001 | Registrar el trabajo y conservar su origen |
| PROP-RF-NP-002 | Contextualizarlo en el POA y vincularlo con la estrategia |
| PROP-RF-NP-003 | Revisar y decidir la solicitud |
| PROP-RF-NP-004 | Estimar y registrar impactos |
| PROP-RF-NP-005 | Registrar ejecución y avances |
| PROP-RF-NP-006 | Adjuntar evidencias y validar resultados |
| PROP-RF-NP-007 | Incorporar mediante un cambio controlado |
| PROP-RF-NP-008 | Consultar y reportar por origen |
| PROP-RF-NP-009 | Notificar pendientes y cambios |

## Flujo de referencia

1. Registrar la necesidad y su contexto.
2. Justificarla y estimar efectos sobre el trabajo vigente.
3. Revisar la solicitud y autorizar, observar o rechazar.
4. Registrar la ejecución real y sus evidencias.
5. Validar el resultado.
6. Resolver su incorporación e impacto en el POA.
7. Consultar el resultado y su historial.

La propuesta inicial contempla borrador, solicitado, observado, autorizado, en ejecución, ejecutado pendiente de validación, cerrado, rechazado y cancelado. La máquina definitiva y las transiciones deben acordarse con los permisos comunes del sistema.

## Reglas que deben preservarse

- Registrar ejecución no equivale a autorizarla ni a aprobar gasto.
- El trabajo informado después de realizarse conserva sus fechas reales; no se simula una autorización previa.
- Registrar una solicitud no modifica automáticamente metas, fechas ni presupuesto aprobados.
- La incorporación formal genera una revisión identificable y conserva la línea base.
- La actividad mantiene su origen no planificado.
- La ejecución y el costo se contabilizan una sola vez.
- El trabajo adicional no mejora automáticamente el cumplimiento institucional.
- Todo porcentaje identifica meta, unidad, período y versión. Sin denominador válido no se asigna un 100 %.
- La evidencia respalda el resultado, pero no sustituye la autorización.
- Los registros aprobados conservan la prohibición de eliminación y su historia de cambios.

## Decisiones pendientes

Deben definirse la clasificación del trabajo, quién autoriza actividad y gasto, la separación entre solicitante y decisor, las reglas de incorporación y vigencia, los cálculos agregados, la documentación mínima y las condiciones de cierre.

El equipo de cinco personas no cambia los seis roles funcionales del SRS. La fecha correspondiente a la semana 6 sigue pendiente de conciliar con el cierre del aula del 2 de diciembre de 2026.

Esta ampliación está en análisis y no representa una función implementada.
