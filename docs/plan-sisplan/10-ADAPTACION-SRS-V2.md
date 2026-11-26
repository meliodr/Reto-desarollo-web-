# Adaptación al SRS v2 del reto PEI y POA

El SRS v2 permite operar PEI y POA por separado o vinculados (págs. 4 y 9). La entrega académica se limita a PEI y POA (pág. 10). El porcentaje calculado para la v1 se conserva como registro histórico.

| Cambio del SRS v2 | Comportamiento de este incremento | Pendiente |
|---|---|---|
| POA autónomo y vínculo opcional (págs. 4 y 9) | Crear y consultar un POA anual desde Planes operativos. El vínculo opcional requiere un PEI publicado que cubra el año. La publicación del PEI ya no crea POA ni cortes. Los registros existentes se conservan. | Productos, actividades, programación, aprobaciones y ejecución del POA. Vínculo detallado de líneas de acción y productos. |
| Estructura estratégica cargada (pág. 9) | La opción Cargar estructura DIGEMAPS registra 6 ejes, 20 objetivos y 20 indicadores desde un catálogo preparado con la hoja Hoja1. Los códigos se derivan de la fila original. La descripción conserva el texto completo del objetivo. Los indicadores sin datos de configuración se muestran como pendientes. | Carga de otros catálogos y edición de los parámetros pendientes de indicadores importados. |
| Revisión y aprobación de PEI (págs. 2, 3 y 7) | El analista envía. El director registra revisión técnica. El comité valida. La autoridad aprueba y el director publica. El PEI puede observarse, corregirse y reenviarse con motivo e historial de cambios de estado. | Confirmar la representación del comité, que el flujo nombra pero la RACI omite. Se usa un rol específico para este paso. |
| Múltiples instituciones (págs. 4 y 10) | Se prepararon los roles de acceso del flujo PEI. | Gestión de instituciones, asignaciones por dependencia, aislamiento de datos por institución y configuración de módulos. |
| Seguimiento mensual, trimestral, semestral y anual (págs. 7 y 8) | Se conservan las evaluaciones históricas existentes. | Programación y seguimiento con las frecuencias de la v2, evidencias y evaluación. |

## Verificación del incremento

Aplica la migración 0006 con el comando habitual de la API. Las instrucciones de la v1 fueron archivadas como referencia en el [registro del primer incremento](06-PRIMER-INCREMENTO.md).

Con Keycloak habilitado, asigna a cuentas de prueba el rol de cliente correspondiente: AnalistaPlanificacion, DirectorPlanificacion, ComiteEstrategico, MaximaAutoridad y Auditor. Si ya tienes un realm importado, agrega esos roles al cliente antes de asignarlos. Para reproducir las verificaciones, ejecuta `python3 tests/flujo_smoke.py` con los tokens respectivos en `SISPLAN_ANALISTA_TOKEN`, `SISPLAN_DIRECTOR_TOKEN`, `SISPLAN_COMITE_TOKEN`, `SISPLAN_AUTORIDAD_TOKEN` y `SISPLAN_AUDITOR_TOKEN`. Usa una base desechable y datos de prueba. La prueba registra un POA independiente, verifica la carga, recorre observación y corrección y comprueba el vínculo opcional al PEI publicado.

El 26 de noviembre se verificó la compilación de .NET, la aplicación y reaplicación de las migraciones en una base de prueba y el flujo HTTP con cinco cuentas de Keycloak y PostgreSQL 18. Angular aprobó 20 pruebas, incluida la creación de un POA autónomo. El vínculo al PEI se solicita al crear el POA y valida el período en la API.
