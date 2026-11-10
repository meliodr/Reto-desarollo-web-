# Método de trabajo — SISPLAN

## Propósito

Coordinar un equipo académico de cuatro integrantes y construir una entrega trazable respecto del SRS y la consigna. El método toma aprendizajes de FuelTrack sobre documentación, integración y pruebas; no incorpora su lógica de negocio a este proyecto.

La asignación nominal de los cuatro integrantes está pendiente. Los roles funcionales del sistema y las responsabilidades del equipo de desarrollo son conceptos distintos.

## Fuente de verdad y decisiones

- Conservar el SRS y la consigna como fuente de requisitos.
- Identificar cada requisito por su código original, cuando exista.
- Separar requisitos confirmados, propuestas del equipo, decisiones aceptadas y dudas pendientes.
- Registrar las decisiones que afecten alcance, cálculos, seguridad, datos o arquitectura con motivo, alternativas consideradas, fecha y evidencia de aceptación.
- No asumir que una solución empleada en FuelTrack es obligatoria para el docente o para SISPLAN.

## Secuencia de trabajo

1. Desglosar el requisito y definir su criterio observable de aceptación.
2. Identificar dependencias, datos, responsables y reglas pendientes.
3. Diseñar el cambio mínimo que permita completar un flujo de negocio.
4. Implementar en una rama acotada.
5. Verificar reglas, permisos, persistencia y casos límite afectados.
6. Abrir pull request y revisar código, documentación y evidencia.
7. Integrar el cambio aprobado y actualizar el estado.
8. Repetir el flujo integrado antes de una entrega o demostración.

No empezar con pantallas aisladas que simulen funcionalidades sin identificarlas como propuesta. La demostración final debe distinguir funciones reales, prototipos y pendientes.

## Ramas y pull requests

- Usar una rama por tarea o cambio coherente, por ejemplo `docs/...`, `feature/...` o `fix/...`.
- Mantener los cambios pequeños y evitar mezclar funciones no relacionadas.
- Acordar la propiedad de archivos compartidos y coordinar cambios de modelos, contratos y migraciones.
- Actualizar la rama con la base vigente antes de integrar y resolver conflictos sin sobrescribir el trabajo ajeno.
- Describir en cada PR el problema, el resultado, los requisitos afectados y la validación realizada.
- Solicitar revisión de otro integrante antes de fusionar. Si excepcionalmente se integra sin ella, registrar el motivo y la revisión pendiente.
- No considerar configuradas protecciones automáticas de ramas ni CI hasta comprobarlas en el repositorio.
- No publicar contraseñas, tokens, claves privadas ni configuraciones personales.

## Evidencia de verificación

Cada evidencia debe indicar, cuando corresponda:

| Campo | Contenido |
|---|---|
| Requisito | Código del SRS o referencia concreta de la consigna |
| Cambio | Rama, PR y commit |
| Fecha y entorno | Cuándo y dónde se verificó |
| Caso | Acción, datos y resultado esperado |
| Resultado | Aprobado, fallido, bloqueado o no ejecutado |
| Respaldo | Prueba, registro, captura o documento pertinente |
| Límites | Qué no demuestra esa evidencia |

Una compilación correcta no acredita por sí sola el funcionamiento del flujo. Una prueba local no acredita disponibilidad, entrega externa ni condiciones de producción.

## Verificaciones a priorizar en SISPLAN

La selección concreta de herramientas se decidirá con la arquitectura. Independientemente de ellas, cubrir:

- Vinculación PEI–POA–producto–actividad y restricciones por responsable o área.
- Generación anual y períodos cuatrimestrales.
- Separación entre datos programados y ejecutados.
- Cálculo y agregación de indicadores con distintas unidades.
- Metas en cero, valores vacíos, decimales y límites de cumplimiento.
- Evidencias obligatorias, revisión, aprobación y planes correctivos.
- Cambios simultáneos y lecturas obsoletas al aprobar o evaluar.
- Permisos aplicados en el servidor, además de la interfaz.
- Conservación de registros aprobados, versiones y auditoría.
- Persistencia, exportaciones y reproducción del flujo completo.

## Estados de seguimiento

| Estado | Significado |
|---|---|
| Pendiente | No iniciado |
| En análisis | Se están precisando reglas y aceptación |
| En desarrollo | Hay implementación en curso |
| Implementado sin verificar | Hay código, pero falta evidencia suficiente |
| Verificado local | Evidencia satisfactoria en un entorno identificado |
| Parcial | Solo una parte del requisito está respaldada |
| Bloqueado | Existe una dependencia concreta que impide avanzar |
| Pendiente externo | Requiere servicio, equipo, aprobación o entorno externo |
| Cerrado | Se cumple el criterio de aceptación acordado con evidencia |

No calcular un porcentaje global de avance sin definir previamente qué se cuenta, sus pesos y sus criterios de cierre. Documentación terminada y funcionalidad construida deben medirse por separado.
