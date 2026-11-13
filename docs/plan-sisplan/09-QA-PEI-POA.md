# QA del recorrido PEI–POA

Revisión del 13 de noviembre de 2026 sobre el [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1). La línea de partida fue el commit `a25a828`.

| Caso | Resultado de la revisión |
|---|---|
| Guardado lento del PEI | En Chromium, al retrasar la respuesta de guardado el campo de visión quedaba editable y lo escrito se perdía al confirmar. Los campos se bloquean durante la solicitud y se habilitan al responder. Hay una prueba Angular con respuesta diferida. |
| Precisión de indicadores | La línea base, la meta y la medición viajan como decimales en texto y PostgreSQL los guarda como `numeric`. Se comprobó que `999999999999.0003` llega intacto y aparece igual al volver a abrir el indicador. La API sigue aceptando números JSON de clientes existentes. |
| Motivo de devolución | Después de devolver un PEI y volver a abrirlo desde la cuenta que lo envió, se puede consultar el último motivo junto a las acciones de revisión. La prueba HTTP comprueba que el motivo se recupera. |
| Fechas y mediciones | La API señala el inicio, el fin o ambas fechas según cuáles excluyan mediciones existentes. En 12 carreras de la revisión inicial entre registrar una medición y ajustar fechas, una sola escritura prosperó y el período siguió siendo válido. |
| Accesibilidad | En Chromium, el filtro de tipo conserva su nombre accesible a 1280, 1200 y 390 px. Axe no reportó infracciones en el recorrido móvil de formulario de indicador que se evaluó. |
| Contrato | El documento pasó la validación estructural de OpenAPI 3.1. La seguridad se declara de forma global para el entorno institucional y se describe la excepción local de borradores. |

## Pruebas que debe repetir el revisor

1. Ejecutar los [pasos de arranque y pruebas](06-PRIMER-INCREMENTO.md) con datos de prueba. Confirmar que un borrador anterior sobrevive al aplicar y volver a aplicar las migraciones.
2. Usar dos planificadores y una cuenta sin el rol. Enviar, devolver con un motivo, revisar el motivo con el autor, reenviar, aprobar con la otra cuenta y publicar.
3. Comprobar cuatro POA para un PEI ordinario y cinco para uno extraordinario. Repetir la publicación y comprobar que los identificadores se conservan. Verificar vigencia y cuatrimestres en un PEI de fechas parciales.
4. En un borrador con eje activo, guardar un indicador y una medición con el valor `999999999999.0003`. Recargar y confirmar el valor. Mover la fecha de inicio de modo que excluya la medición y comprobar el mensaje de validación.

## Hallazgo abierto

Si un PEI ya aprobado entra en conflicto con las fechas de otro PEI publicado, la publicación se rechaza y se preservan el registro y los POA existentes. El plan aprobado queda sin una acción de corrección disponible. El equipo debe definir cómo abrir una nueva revisión o cancelar ese caso conservando la aprobación original.

La verificación cubre la formulación y la generación de POA. La activación y el registro de ejecución están pendientes de aceptación en RF-005 y RN-001. RF-012 queda parcial por avances y emisión con evidencia. RNF-001 queda parcial por MFA y el alcance de los demás roles.
