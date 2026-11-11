# Método de trabajo

## Organización

El equipo está formado por **cinco integrantes**. La distribución de tareas y sus responsables está pendiente de registrar. Los seis roles previstos para los usuarios del sistema no corresponden a los puestos del equipo de desarrollo.

La revisión de [FuelTrack](https://github.com/Rowlistp/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE) aportó referencias para organizar fases, revisar cambios y relacionar requisitos con pruebas. La planificación de este proyecto se hará a partir de su propia consigna y SRS.

## Preparación de cada tarea

Antes de implementar una función se debe registrar:

- El requisito de origen y el resultado esperado.
- Las reglas y los datos que necesita.
- Las dependencias con otras tareas.
- El responsable y el criterio de aceptación.
- Las dudas que puedan cambiar su comportamiento.

Las actividades no planificadas se tratarán como una ampliación solicitada. Sus reglas deben definirse antes de incluirlas en el desarrollo. Las decisiones de alcance y arquitectura se registran en [DECISIONES.md](DECISIONES.md).

## Desarrollo y revisión

1. Crear una rama para una tarea concreta, con prefijo `docs/`, `feature/` o `fix/`.
2. Coordinar los cambios en modelos, contratos, migraciones y archivos compartidos.
3. Implementar y verificar el flujo afectado.
4. Abrir un pull request con el requisito, los cambios y las pruebas realizadas.
5. Solicitar revisión de otro integrante y resolver sus observaciones.
6. Integrar el cambio y actualizar su estado.

Los commits deben agrupar cambios relacionados. Al resolver conflictos se debe conservar el trabajo vigente de los demás integrantes. Las contraseñas, claves y configuraciones privadas quedan fuera del repositorio.

Estas son reglas de trabajo del equipo. Las protecciones de ramas y la integración continua se registrarán cuando estén configuradas.

## Verificación

Las pruebas deben cubrir las reglas del servidor, los permisos, la persistencia y el uso de la interfaz. Se priorizarán los flujos completos de planificación, avance, evaluación y reporte, junto con estos casos:

- Metas en cero, campos vacíos, porcentajes y unidades diferentes.
- Evidencias obligatorias y planes correctivos.
- Aprobaciones o evaluaciones realizadas desde dos sesiones.
- Restricciones de edición y eliminación de registros aprobados.
- Historial de cambios, exportaciones y recuperación de datos guardados.

Cada resultado debe conservar la siguiente información:

| Dato | Registro |
|---|---|
| Requisito | Código del SRS o referencia de la ampliación |
| Versión | PR o commit comprobado |
| Entorno | Fecha, aplicación y servicios utilizados |
| Caso | Datos, acción y resultado esperado |
| Resultado | Aprobado, fallido, bloqueado o no ejecutado |
| Evidencia | Prueba, registro o captura pertinente |
| Alcance | Condiciones verificadas y pendientes |

Las comprobaciones del portal de documentación se registran aparte de las pruebas del sistema institucional. Los resultados locales y las pruebas de servicios externos también deben distinguirse.

## Seguimiento

| Estado | Uso |
|---|---|
| Pendiente | La tarea no ha comenzado |
| En análisis | Se están definiendo reglas y aceptación |
| En desarrollo | La implementación está en curso |
| Implementado sin verificar | Existe código y falta comprobarlo |
| Verificado | Hay evidencia del resultado en un entorno identificado |
| Parcial | Queda una parte del requisito pendiente |
| Bloqueado | Una dependencia identificada impide continuar |
| Cerrado | Se cumple el criterio de aceptación acordado |

El avance se registrará por tarea y requisito. Un porcentaje general requerirá definir primero qué se mide y cómo se considera terminado.

## Calendario

La referencia actual del aula indica cierre el **2 de diciembre de 2026 a las 23:59**. El objetivo mencionado para la **semana 6** debe conciliarse con esa fecha antes de fijar el calendario del equipo.
