# Reglas operativas de SISPLAN

**Base de trabajo v1.0 · 12 de noviembre de 2026.** Los requisitos y los identificadores se conservan en la [matriz de trazabilidad](05-TRAZABILIDAD.md). Este documento fija las decisiones del diseño del equipo donde el SRS deja alternativas o vacíos. Si el maestro da una aclaración distinta, actualizar la regla, el contrato y su prueba en el mismo cambio.

## Institución, alcance y permisos

La primera versión administra **una institución con varias dependencias**. Una cuenta puede tener varios roles y asignaciones. La API exige un permiso explícito por acción y verifica el alcance de la dependencia del registro, incluidas consultas, evidencias y exportaciones. El identificador de usuario es el par emisor y sujeto del proveedor de identidad. Una cuenta desactivada o sin asignación recibe acceso denegado aunque conserve una sesión del proveedor.

| Rol | Puede hacer | Alcance |
|---|---|---|
| Administrador | Administrar catálogos, unidades, usuarios y asignaciones. Configurar indicadores. Publicar evaluaciones emitidas. Consultar auditoría | Institucional |
| Planificador | Formular, revisar, aprobar y publicar PEI. Crear y aprobar productos, aprobar versiones de POA, consolidar resultados y elaborar informes. Autorizar incorporación de trabajo adicional | Institucional. La aprobación de una versión exige un planificador distinto de quien la envió |
| Responsable de Área | Formular actividades y programación, registrar avances, indicadores y evidencias, presentar correctivos y solicitudes de trabajo adicional | Dependencias asignadas |
| Evaluador | Revisar evidencias, observar o validar avances, emitir resultados y verificar correctivos | Dependencias asignadas. Una cuenta no valida un avance que ella registró |
| Consultor | Consultar planes y resultados publicados y descargar reportes de su alcance | Dependencias asignadas o ámbito institucional asignado |
| Consulta Ejecutiva | Consultar dashboard de resultados publicados | Ámbito institucional asignado. El detalle conserva las restricciones de acceso |

Las asignaciones de la aplicación se gestionan desde SISPLAN. La cuenta de identidad se crea en Keycloak por el operador designado. Las cuentas de demo representan los seis roles y dos planificadores distintos. Los roles del sistema son independientes de los cinco frentes de desarrollo.

## PEI, POA y versiones

| Decisión | Comportamiento |
|---|---|
| Período | Fechas inclusivas `YYYY-MM-DD` y zona `America/Santo_Domingo`. El período se deriva del año de inicio al de fin. Ordinario = cuatro años calendario inclusivos, extraordinario = cinco. Se admiten años parciales y se conserva la fecha exacta. |
| Estados del PEI | `borrador → en_revision → aprobado → publicado`. Revisar puede devolver a `borrador` con motivo. La persona que aprueba es distinta de quien envía. Publicar requiere aprobación y ejes, objetivos e indicadores vinculados. |
| Vigencia | Un PEI publicado es vigente en las fechas que cubre. Dos PEI distintos de la institución no pueden tener intervalos publicados solapados. El histórico se consulta por fecha y versión. |
| Generación | La primera publicación crea, en una transacción, un POA por año cubierto y tres evaluaciones por POA. Unicidad por PEI/año y POA/cuatrimestre. Repetir el evento recupera los mismos registros. |
| Años futuros y parciales | Se pueden formular POA futuros bajo un PEI publicado para ese año. Un avance requiere actividad, versión aprobada y fecha de ejecución dentro de la vigencia del PEI y del año POA. Un corte sin días de vigencia queda identificado como fuera de vigencia. Los tres cortes conservan sus meses oficiales. |
| POA y productos | El POA sigue `formulacion → en_revision → aprobado → en_ejecucion → cerrado`. Entra en ejecución en su intervalo vigente. El planificador lo cierra al finalizar su intervalo con los tres cortes publicados o marcados fuera de vigencia. Los productos se envían y se aprueban individualmente. El planificador aprueba una versión de POA con productos aprobados y programa su entrada en vigor. La actividad exige responsable, fechas, producto y unidad. El producto referencia un indicador de la versión estratégica correspondiente, del que se derivan eje y objetivo. |
| Cambios | Una entidad aprobada se conserva. Para corregirla se crea otra revisión con motivo, referencias a la anterior y fecha efectiva posterior al último corte publicado. Una revisión del mismo PEI conserva sus fechas de vigencia. Las versiones de POA y cortes históricos conservan su referencia al PEI y siguen consultables. Los ejes se inactivan en la nueva versión. |
| Concurrencia | Una escritura sobre un registro existente exige `If-Match` de su último `ETag`. Ausencia `428`, versión obsoleta `412`, estado incompatible `409`, campo inválido `422`. La actualización y su auditoría se confirman juntas. |

El POA organiza programación anual, mensual y cuatrimestral. El equipo registra metas de enero a diciembre según el método del indicador. La meta cuatrimestral y anual se deriva del mismo conjunto por período. Los encabezados de trimestre del Excel son referencias visuales y no cambian los cuatrimestres enero–abril, mayo–agosto y septiembre–diciembre. Un dato vacío se guarda como pendiente. Un cero explícito representa que no se programó una cantidad para ese mes.

## Avances, evaluación y publicación

1. El responsable registra un avance por actividad y mes, con cantidades físicas, gasto del mes, comentario y evidencia. Una revisión del avance sustituye la revisión anterior para los cálculos del mes. Cada cantidad representa el incremento de ese mes, salvo mediciones de tipo último valor o razón, que siguen el catálogo.
2. El evaluador observa o valida la revisión vigente. Una devolución exige motivo y permite corregir y reenviar. El planificador verifica la consolidación de su ámbito con las revisiones validadas.
3. Los estados de una evaluación son `generada → en_revision → emitida → publicada`. Para emitir, el evaluador debe verificar evidencia de cada resultado evaluado y congelar las versiones de programación, avances y evidencias utilizadas. Las métricas pendientes se muestran con su causa. La emisión exige al menos un resultado evaluable con evidencia. Un corte fuera de vigencia se justifica por fechas y queda en el historial.
4. La emisión de un resultado rojo crea su plan correctivo obligatorio en la misma transacción. Se asigna al responsable del área y el evaluador verifica su cierre. El correctivo tiene causa, acción, responsable, fecha límite, evidencia y estado `pendiente → en_ejecucion → en_revision → cerrado`. La publicación muestra los correctivos pendientes. No exige que ya estén ejecutados.
5. El administrador publica un resultado emitido. Dashboard y reportes oficiales consultan cortes publicados. Una corrección posterior emite una nueva revisión con motivo y vínculo al corte original. El historial conserva qué versión alimentó cada reporte.

## Cálculo, consolidación y presupuesto

| Tema | Regla para implementar y probar |
|---|---|
| Base válida | Con programado positivo y ejecución validada, el cumplimiento es `ejecutado / programado × 100`. Programado cero o ausente se presenta como «Sin base de cálculo». Sin ejecución validada se muestra «Pendiente». Si se registra y valida ejecución cero con meta positiva, el resultado es 0 %. |
| Avance y sobreejecución | El avance porcentual se limita a 100 % y la API rechaza un avance declarado superior. La cantidad real y el cumplimiento calculado se conservan completos. Ejemplo: 12 de 10 implica avance 100 % y cumplimiento 120 %. |
| Semáforo | Rojo menor a 70 %. Amarillo desde 70 % hasta menos de 90 %. Verde desde 90 %. Clasificar con valores completos. Mostrar hasta dos decimales truncados para evitar que un redondeo aparente cruzar el umbral. «Sin base» y «Pendiente» tienen etiquetas propias. |
| Métodos de medición | `suma` para cantidades acumulables, `ultimo` para niveles al cierre y `razon` para tasas. La razón almacena numerador y denominador, los consolida por separado y divide solo con denominador positivo. La fórmula explicativa del catálogo es texto. El cálculo lo ejecutan esos métodos definidos en la API. |
| Consolidación | Calcular primero cada indicador con mediciones comparables, unidad y corte coherentes. Agrupar para cada vista por eje, objetivo, área e institución. Cada indicador aporta una sola puntuación por grupo. Media simple de puntuaciones válidas con máximo 100 por aporte, acompañada de válidos/esperados. Si falta un resultado esperado, indicar cobertura parcial. Sin puntuaciones válidas, mostrar «Sin base». |
| Vínculos estratégicos | El indicador estratégico se mide en su unidad y frecuencia. El cumplimiento de un producto no sustituye automáticamente la medición del indicador. Ejemplo: jornadas realizadas y porcentaje de personal capacitado son medidas distintas. |
| Presupuesto | DOP, montos decimales con dos posiciones. Separar estimado, autorizado y ejecutado. Solo se suman importes de la misma moneda, período y alcance. Ejecución presupuestaria = gasto validado / monto autorizado × 100, con las mismas reglas de base válida. El exceso se muestra como desviación. En la plantilla del reto, el presupuesto se asocia a solicitudes de insumos y la columna U excluye RR. HH., flota adherida al puesto y aumentos salariales. |
| Reportes | PEI, POA, cuatrimestral, anual, indicadores y presupuesto. PDF y XLSX utilizan la misma consulta autorizada, con versión y corte visibles. Las cifras agregadas deben cuadrar con el dashboard al aplicar los mismos filtros. |

Para avance, metas y cantidades usar decimales; las cantidades admiten hasta cuatro posiciones. El cálculo de referencia de la demo usa una meta de tres y ejecución de dos, con evidencia validada. El resultado es **66,66 % mostrado, rojo y con correctivo**. El plan muestra los casos límite de 70 %, 90 %, base cero y sobreejecución.

| Caso mínimo | Resultado esperado |
|---|---|
| Meta 10, ejecutado validado 0 | 0 %, rojo y correctivo |
| Meta 10, ejecutado 7 | 70 %, amarillo |
| Meta 10, ejecutado 8,9999 | 89,99 % mostrado, amarillo |
| Meta 10, ejecutado 9 | 90 %, verde |
| Meta 10, ejecutado 12 | 120 % de cumplimiento, 100 % de avance |
| Meta cero o vacía | Sin base de cálculo, sin semáforo ni correctivo automático |
| Meta positiva y avance sin validar | Pendiente, se informa como faltante en la cobertura |

## Trabajo no planificado, proyectos, riesgos y alertas

- **Trabajo no planificado.** El responsable registra motivo, área, responsable, fechas y estimación. La autorización sigue `solicitada → autorizada` o `devuelta/rechazada`. La ejecución sigue `no iniciada → registrada → validada` o `observada`, y se puede corregir y reenviar. Para hechos que ya ocurrieron se registra la fecha real y se mantiene la indicación «Sin autorización previa». El evaluador valida ejecución y evidencia. El planificador decide si incorpora el trabajo en una nueva revisión del POA. Un cambio de monto requiere la capacidad específica de autorización presupuestaria asignada al planificador y deja motivo y diferencias. La revisión tiene efecto prospectivo. Las ejecuciones anteriores conservan su propio corte y se muestran como trabajo adicional en el histórico. Un avance conserva su identificador y se contabiliza una vez, aunque se vincule a una actividad incorporada.
- **Proyectos.** Portafolio contiene programas y programa contiene proyectos. Cada proyecto se vincula a un objetivo, tiene responsable, fechas, hitos y entregables. Puede vincular actividades existentes del POA. Las cifras financieras se consultan desde esas actividades para evitar duplicación. El planificador gestiona el catálogo y el responsable asignado informa su avance.
- **Riesgos.** Un riesgo estratégico se vincula a un objetivo y uno operativo a una actividad o proyecto. Probabilidad e impacto van de 1 a 5. Su producto clasifica bajo 1–4, medio 5–14 y alto 15–25. El responsable registra tratamiento, acción, fecha y evidencia. Riesgos altos requieren mitigación antes de cerrar el riesgo. El planificador valida el tratamiento.
- **Alertas.** Bandeja interna. Actividad vencida para el responsable de área, evaluación pendiente para el evaluador asignado, indicador desactualizado para su responsable e incumplimiento para el responsable de área y planificador. Una actividad activa vence al día siguiente de su fecha fin. La evaluación queda pendiente al finalizar el cuatrimestre si el corte contiene días vigentes y sigue sin publicar. El indicador queda desactualizado si termina su período de frecuencia sin medición validada. El resultado rojo avisa al emitirse. Un proceso comprueba vencimientos cada 15 minutos y al arrancar. Un evento por tipo, registro, corte y destinatario mientras siga la condición. Una alerta cerrada por resolución queda en historial.
- **Auditoría.** Registrar actor, instante UTC, entidad, acción, valores anterior y nuevo y motivo cuando aplica. El registro se genera en servidor en la transacción de la acción y tiene consulta restringida. Las evidencias y revisiones aprobadas conservan su referencia y archivo histórico.

```mermaid
flowchart TD
    P[Planificador publica PEI] --> O[Generar POA y cortes]
    O --> A[Programar actividad y aprobar POA]
    A --> E[Registrar avance y evidencia]
    E --> V{Evaluador valida}
    V -- Observado --> E
    V -- Validado --> R[Emitir resultado]
    R --> C{Cumplimiento menor a 70 %}
    C -- Sí --> K[Crear correctivo]
    K --> D[Administrador publica el corte]
    C -- No --> D
    D --> I[Dashboard e informes]
```

[Especificación del maestro, Excel y consigna](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/#documentos)
