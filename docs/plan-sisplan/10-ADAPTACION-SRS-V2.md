# Adaptación al SRS v2 del reto PEI y POA

El SRS v2 permite operar PEI y POA por separado o vinculados (págs. 4 y 9). La entrega académica se limita a PEI y POA (pág. 10). El porcentaje calculado para la v1 se conserva como registro histórico. La aclaración del maestro del 26 de noviembre permite dejar el presupuesto vacío o registrar un valor de referencia.

| Cambio del SRS v2 | Comportamiento de este incremento | Pendiente |
|---|---|---|
| POA autónomo y vínculo opcional (págs. 4 y 9) | Crear y consultar un POA anual desde Planes operativos. El vínculo opcional requiere un PEI publicado que cubra el año. La publicación del PEI ya no crea POA ni cortes. Los registros existentes se conservan. | Productos, actividades, programación, aprobaciones y ejecución del POA. Vínculo detallado de líneas de acción y productos. |
| Presupuesto de referencia opcional (programación del POA, pág. 9, con aclaración del maestro) | Al crear un POA de cualquier modalidad se puede registrar un estimado en RD$ o dejar el campo vacío. El listado muestra «Sin registrar» si no hay importe. Un cero explícito se conserva como RD$ 0.00. | Detalle presupuestario por producto o actividad y seguimiento financiero. |
| Estructura estratégica cargada (pág. 9) | La opción Cargar estructura DIGEMAPS registra 6 ejes, 20 objetivos y 20 indicadores desde un catálogo preparado con la hoja Hoja1. Los códigos se derivan de la fila original. La descripción conserva el texto completo del objetivo. Los indicadores sin datos de configuración se muestran como pendientes. | Carga de otros catálogos y edición de los parámetros pendientes de indicadores importados. |
| Revisión y aprobación de PEI (págs. 2, 3 y 7) | El analista envía. El director registra revisión técnica. El comité valida. La autoridad aprueba y el director publica. El PEI puede observarse, corregirse y reenviarse con motivo e historial de cambios de estado. | Confirmar la representación del comité, que el flujo nombra pero la RACI omite. Se usa un rol específico para este paso. |
| Múltiples instituciones (págs. 4 y 10) | Se prepararon los roles de acceso del flujo PEI. | Gestión de instituciones, asignaciones por dependencia, aislamiento de datos por institución y configuración de módulos. |
| Seguimiento mensual, trimestral, semestral y anual (págs. 7 y 8) | Se conservan las evaluaciones históricas existentes. | Programación y seguimiento con las frecuencias de la v2, evidencias y evaluación. |

## Presupuesto de referencia

El valor se captura a nivel del POA anual en este incremento. Es un estimado informativo. Los productos de `Matriz POA!U12:U22` del archivo POA TI 2026 V2 tienen el presupuesto vacío. La aplicación conserva esa ausencia en los POA existentes al aplicar la migración `0007`. Los ejemplos que se usen en la demostración se identifican como valores de referencia.

El formulario acepta hasta doce dígitos enteros y dos decimales, con coma o punto decimal. La API recibe `presupuestoReferencia` como texto con punto decimal y devuelve dos decimales. Cuando el importe se omite, la API devuelve `null`. [Contrato de la API](../../contracts/pei.openapi.yaml)

## Verificación del incremento

1. Sigue el [arranque local](06-PRIMER-INCREMENTO.md#arranque-local) y aplica las migraciones hasta la `0007` mediante `--migrate`.
2. Para el recorrido autenticado, arranca Keycloak con el perfil `institucional` de Compose y habilita `Sisplan__Oidc__Url=http://127.0.0.1:8080` en la API. Asigna estos roles de cliente a cuentas de prueba: `AnalistaPlanificacion`, `DirectorPlanificacion`, `ComiteEstrategico`, `MaximaAutoridad` y `Auditor`. Si el realm ya existía, agrega primero los roles que falten al cliente.
3. Ejecuta `python3 tests/flujo_smoke.py` en una base desechable con los cinco tokens respectivos en `SISPLAN_ANALISTA_TOKEN`, `SISPLAN_DIRECTOR_TOKEN`, `SISPLAN_COMITE_TOKEN`, `SISPLAN_AUTORIDAD_TOKEN` y `SISPLAN_AUDITOR_TOKEN`. Ejecuta `npm test -- --watch=false` desde el directorio `frontend`.

| Comprobación del 26 de noviembre de 2026 | Resultado |
|---|---|
| Carga DIGEMAPS, roles, observación, corrección y publicación del PEI | Prueba HTTP aprobada con PostgreSQL 18 y cinco cuentas de Keycloak. La publicación deja vacío el listado de POA vinculados hasta que se crea uno. |
| Alta independiente o vinculada y presupuesto opcional | Prueba HTTP aprobada. Acepta campo omitido, `null`, cero y un monto de referencia con centavos. Se verificó la recuperación en el listado y el rechazo de negativos, exceso de decimales y formato inválido. |
| Formulario y regresiones de Angular | 21 pruebas aprobadas y compilación correcta. El formulario acepta la coma decimal y distingue estimado y ausencia. |
| Contrato | Se verificaron las referencias y esquemas JSON de solicitud y respuesta para el presupuesto. |
| Migración de POA previo | Se aplicó la migración `0007` sobre un POA existente y se repitió. El listado devolvió el mismo registro con presupuesto `null`. |
