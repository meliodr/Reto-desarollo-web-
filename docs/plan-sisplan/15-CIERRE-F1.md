# F1 · Formulación y publicación del PEI

F1 usa los permisos, la institución activa y la auditoría de F0. Desde **Planes estratégicos**, un analista puede abrir el borrador, cargar DIGEMAPS, completar el mapa y enviar el plan a revisión. La consulta de una versión oficial ofrece su mapa y los motivos del historial.

| Subfase | Criterio reproducible |
|---|---|
| F1.1 · Vigencia y catálogo | Cuatro años calendario inclusivos en el PEI ordinario y cinco en el extraordinario. La primera carga de DIGEMAPS registra seis ejes, veinte objetivos y veinte indicadores en una transacción. Los reintentos conservan las filas originales. Un error intermedio revierte los datos y su auditoría. Si se inicia la carga desde varias sesiones a la vez, una sola registra el catálogo. |
| F1.2 · Mapa y metas | El mapa muestra ejes, objetivos, resultados, indicadores, metas, líneas de acción, riesgos y el estado activo de la estructura. El analista puede corregir el texto de un objetivo en la versión editable y conservar sus vínculos. Sólo los indicadores configurados admiten nuevas mediciones. |
| F1.3 · Revisión y aprobación | El analista envía. El Director de Planificación revisa. Un representante del Comité valida. Máxima Autoridad aprueba. El Director publica. Las devoluciones conservan autor, fecha y motivo. Cada escritura con ETag rechaza una versión obsoleta. |
| F1.4 · PEI oficial | Una institución con solo PEI puede publicar, consultar por fecha y abrir el mapa exacto de una versión. La publicación y sus reintentos generan cero POA. |

## Origen del catálogo

El cotejo de **Hoja1, A4:E23** compara el eje, el objetivo completo, el resultado esperado, el indicador y la línea de acción de cada fila. El código `DIG-04` a `DIG-23` conserva su número en la hoja. El título de un objetivo respeta el límite de 200 caracteres y su descripción conserva íntegro el texto fuente, incluido el ordinal. El archivo original se conserva como referencia del proyecto. SHA-256 `afb42c7240fea036b27d03e100a0f7fab678da177120409af386efeaffc033f8`. [Catálogo del repositorio](../../backend/Catalogos/digemaps.json)

## Reglas de formulación y publicación

- Un eje activo necesita al menos un objetivo activo. Cada objetivo activo necesita un indicador vinculado. Los indicadores precargados pueden permanecer **Pendientes de configuración** y quedan fuera del registro de nuevas mediciones hasta estar listos. Antes de confirmar la publicación, el Director ve cuántos indicadores de DIGEMAPS del mapa activo siguen pendientes. Para aprobar o publicar una nueva versión, completa los indicadores manuales de los objetivos activos, incluidos los creados antes de F1.
- El método **Valor directo** admite una línea base vacía. **Variación absoluta** requiere línea base. **Variación porcentual** requiere una línea base distinta de cero. La fórmula, unidad, frecuencia, sentido y meta completan la configuración. F3 consume estos datos para el seguimiento y la evaluación.
- Los riesgos se registran en el objetivo. La puntuación es probabilidad × impacto, con valores de uno a cinco. Un riesgo alto requiere tratamiento, acción, responsable, fecha y un enlace a la evidencia para cerrarse.
- Al abrir una revisión de un PEI publicado, el analista registra el motivo y trabaja sobre una nueva versión con la vigencia de la oficial. La publicación anterior sigue disponible para consulta y para el POA durante la revisión. El historial permite consultar también los ejes, objetivos, riesgos, indicadores y mediciones de una versión publicada anterior. Las mediciones y los POA existentes siguen vinculados a la versión original. La sucesora pasa por el mismo flujo antes de convertirse en la versión oficial.
- La fecha inicial y la final forman parte de la vigencia. Al buscar un año de cuatro dígitos, el listado encuentra los PEI cuyo intervalo incluye ese año. Si sus intervalos coinciden siquiera en un día, dos PEI distintos de la misma institución no pueden quedar publicados a la vez.

## Verificación

La verificación de GitHub ejecuta las regresiones de F0, el contrato OpenAPI y las pruebas HTTP de carga, riesgos, indicadores, concurrencia, versiones y exclusión de POA. En Chromium se consulta la versión publicada, su historial y el mapa a 390 y 1280 píxeles. Para repetir el cotejo de la hoja fuente, ejecuta `python3 tests/f1_catalogo.py --excel RUTA_DEL_XLSX`.

[Contrato OpenAPI](../../contracts/pei.openapi.yaml) · [Plan y criterios de F1](11-PLAN-POR-FASES-SRS-V2.md#subfases-de-f1)
