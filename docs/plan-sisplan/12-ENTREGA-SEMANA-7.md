# Entrega de SISPLAN para la semana 7

La **semana 4 está en curso**. Al terminar empieza la semana 5. La meta es llegar **al inicio de la semana 7** con un candidato que haya pasado la aceptación, con accesos, documentación y demo listos. La semana 7 queda para presentar y atender incidencias de entrega. La consigna anterior registraba el 2 de diciembre de 2026 a las 23:59. La fecha oficial vigente debe verificarse en el aula durante la semana 4. Si se mantiene una fecha anterior a la meta interna, se escala al equipo y al maestro en cuanto se confirme.

[Plan por fases y pruebas](11-PLAN-POR-FASES-SRS-V2.md) · [Avance comprobado del PR](10-ADAPTACION-SRS-V2.md)

## Capacidad y responsables

Estimación inicial de planificación, pendiente de disponibilidad real. Para cinco integrantes a **10 horas por persona por semana**, hay 50 horas brutas por semana. Con una reserva del 20 % para bloqueos, correcciones y revisiones, quedan **40 horas netas en la semana 5 y 40 en la semana 6**. Las estimaciones incluyen implementación, revisión del PR y pruebas del frente. Para lo que resta de la semana 4, se estiman 8–12 horas de equipo para decisiones, alcance y preparación, sujetas a las horas que todavía tenga cada integrante.

| Frente propuesto | Semana 5 | Semana 6 |
|---|---|---|
| Plataforma y seguridad | **8–10 h.** Entorno común, dos instituciones, cuentas, permisos, migración y aislamiento inicial. | **6–8 h.** Completar permisos y auditoría del alcance, migración y recuperación en el entorno candidato. |
| PEI y datos | **5–7 h.** DIGEMAPS, reglas de publicación y un indicador medible con datos de demo. Apoyo al flujo del POA si queda capacidad. | **6–8 h.** Medición, evaluación y cierre estratégico del recorrido acordado. |
| POA | **9–12 h.** Cortes F2.2a–b, aprobación, publicación y un avance verificable. El límite alto requiere apoyo de otro frente. | **7–9 h.** Completar programación, evaluación, reprogramación y cierre del alcance acordado. |
| Interfaz e integración | **6–8 h.** Formularios del recorrido mínimo, evidencia por URL y contrato de alineación. | **8–10 h.** Alineación, trazabilidad, reportes y revisión visual de los tres modos. |
| QA y entrega | **6–8 h.** Datos de prueba, matriz de aceptación, primer ensayo y registro de resultados. | **8–10 h.** Regresión, accesibilidad, ensayo final, evidencias y paquete del candidato. |
| **Total estimado** | **34–45 h** | **35–45 h** |

Reservar en semana 7 entre **8 y 12 horas de equipo** para verificación de acceso, entrega, presentación y respuestas a incidencias. La capacidad de construcción presupuestada termina al cerrar la semana 6.

Los rangos suman **69–90 h** frente a las 80 h netas del escenario base. Las asignaciones son por frente hasta que el equipo confirme nombres y horas. Al cierre de la semana 4, registrar responsable y revisor por bloque, horas disponibles y dependencias. Si una semana supera sus horas netas, reasignar entre frentes, reducir el alcance de los elementos opcionales según la rúbrica y volver a calcular. Con seis horas brutas por integrante se tendrían solo **24 horas netas por semana**. En ese escenario, el mínimo estimado no cabe y hay que acordar alcance o disponibilidad con el equipo. Si el trabajo obligatorio sigue excediendo la capacidad, comunicar el impacto sobre la fecha con los casos afectados.

## Hitos y pruebas de salida

| Semana | Entrega | Qué se revisa para cerrar | Dependencias |
|---|---|---|---|
| **4 · Cerrar decisiones y preparar** | Confirmar rúbrica y plazo, roles, publicación y MFA. Acordar campos bloqueantes, adjuntos, alineación, reglas de cálculo y alcance mínimo. Separar lo obligatorio de lo opcional y asignar capacidad y revisores. | Lista de decisiones con responsable y estado; casos obligatorios mapeados; horas calculadas; datos y guiones para iniciar ambos módulos. Reproducir la base del PR y registrar qué sigue pendiente. | F0.1 y preparación de F6.1. Se pueden preparar F1 y F2 con los contratos comunes. |
| **5 · Primer recorrido de punta a punta** | Sobre la base mínima aislada, publicar un PEI precargado y recorrer un POA autónomo con **un producto, una actividad, una meta y un responsable**. Enviar, devolver con motivo, corregir, aprobar, publicar, registrar un avance y consultar su evidencia y resultado. Puede usarse una URL de prueba en esta primera demo. | Primer ensayo registrado con el commit probado. Aprobar con presupuesto vacío. Verificar usuario autorizado, denegación de otro rol y acceso denegado entre dos instituciones. Comprobar cálculo, persistencia y trazabilidad del caso pequeño. Revisar la interfaz del flujo a 390 y 1280 píxeles. | F0.1 y porción necesaria de F0.2–F0.4; parte necesaria de F1.1–F1.4 para la publicación de muestra; F2.1, F2.2a–b y F2.3–F2.4; primera ruta de F4.2–F4.3. No depende de F5. |
| **6 · Completar y validar el candidato** | Completar seguimiento y evaluación por módulo y el modo vinculado conforme a la rúbrica. Revisar casos límite, diferencias del Excel y faltantes de alcance, conciliar los reportes y cerrar las pruebas y el paquete sobre un único commit. | Pasan los tres recorridos: PEI autónomo, POA autónomo y vinculado. El 100 % de los criterios obligatorios tiene evidencia aprobada, quedan cero defectos críticos o altos y se confirma instalación, restauración y ensayo final. | F3 y F4 aportan seguimiento; F5 añade trazabilidad y retroalimentación compatible; F6 valida y empaqueta. |
| **7 · Entregar y presentar** | Entregar el candidato y sus evidencias en el canal confirmado por el aula. Presentar los tres recorridos y atender incidencias de acceso o entrega. | Confirmar entrega, accesos y correspondencia entre el commit, el sitio y la evidencia. Si cambia el candidato por una corrección, repetir el caso afectado y la regresión pertinente. | Candidato y paquete aprobados al finalizar la semana 6. |

**Ruta crítica.** Base institucional mínima y permisos → programación mínima del POA → aprobación y publicación → avance con evidencia → ensayo pequeño → regresión y candidato. PEI avanza en paralelo tras el contrato común. Para el modo vinculado, el PEI publicado y el POA en formulación habilitan la captura de la alineación. Un fallo de aislamiento bloquea el cierre del recorrido, aunque se pueda seguir construyendo interfaz y reglas con datos de prueba.

## Prioridades de alcance

| Prioridad | Entra hasta la semana 7 |
|---|---|
| **Necesario para la aceptación** | Recorridos PEI y POA autónomos y vinculados, con los permisos e instituciones del alcance académico; PEI precargado y publicable; POA formulable y aprobable con presupuesto opcional; evidencia y seguimiento reproducibles; trazabilidad, pruebas y demo. El detalle obligatorio de cada bloque lo valida la rúbrica. |
| **Primero en reducir si la rúbrica lo permite** | Pulido visual adicional, filtros y exportaciones extra, automatización de importaciones de datos no exigidas y pantallas de administración general que se puedan sustituir por configuración mínima verificable. Registrar cada recorte con su efecto en los casos. |

Para la primera demo, la administración institucional puede partir de datos configurados de prueba. El aislamiento, la identificación de la institución activa, los permisos y la trazabilidad de las escrituras entran desde ese primer recorrido. El ensayo de la semana 5 aporta evidencia parcial. Si el flujo mínimo queda bloqueado al cierre de esa semana, usar el primer bloque de la semana 6 para resolverlo y reestimar la integración y la entrega antes de tomar trabajo opcional. El cierre de cada fase mantiene los criterios del plan completo.

## Decisiones y reglas para ejecutar

### Roles, publicación y MFA

En F0.1, confirmar con la rúbrica o el maestro los cuatro puntos de decisión: alcance obligatorio, rol del Comité, permiso para publicar y política de MFA. Como base para preparar los casos, conservar el flujo PEI que ya tiene el PR: Director revisa, Comité valida, Máxima Autoridad aprueba y Director publica después de esa aprobación. Para POA, preparar revisión de área, consolidación de Planificación y aprobación institucional, con publicación posterior según el permiso que se confirme. Preparar el caso con segundo factor para las cuentas que aprueban o publican en el entorno de entrega. Registrar si la rúbrica lo exige, con qué roles y qué evidencia se necesita. Un smoke test con token prueba autorización de API. La comprobación de MFA requiere un inicio de sesión real con segundo factor.

### Indicadores y publicación del PEI

Regla de trabajo para F1.2, sujeta a validación en semana 4. La publicación exige la vigencia, la estructura estratégica obligatoria, los nombres y vínculos válidos y las aprobaciones del flujo. Un indicador importado puede publicarse identificado como **Pendiente de configuración** si el catálogo aporta su identidad y vínculo pero le faltan unidad, frecuencia, fórmula, meta o línea base. Ese indicador queda fuera del registro de mediciones y de la evaluación hasta completar los datos requeridos por su método. Para activarlo, registrar unidad, frecuencia, sentido, meta y método de cálculo. La línea base es necesaria cuando la fórmula la usa. En la demo se configura al menos un indicador con datos ficticios identificados y resultado esperado. Probar ambos casos y la denegación para un indicador sin vínculo válido o para una medición sobre un indicador pendiente.

### Primeros cortes del POA

| Corte de F2.2 | Entrega | Prueba de cierre |
|---|---|---|
| **F2.2a · Estructura mínima** | Crear y consultar un producto con una actividad dentro del POA abierto. | Tras recargar, el producto y la actividad mantienen su relación. Se rechaza un producto de otra institución. |
| **F2.2b · Programación para aprobar** | Agregar a esa actividad una meta, unidad, responsable y fechas. | Un POA mínimo puede enviarse a revisión con presupuesto vacío. Se rechazan datos obligatorios faltantes, fechas fuera del período y responsables ajenos. |
| **F2.2c · Ampliación y conciliación** | Completar cronograma, tareas e indicadores operativos exigidos. Cotejar los 11 registros del Excel y documentar faltantes y rótulos repetidos. | Los 11 registros tienen mapeo o tratamiento documentado. Pasan los casos adicionales de programación de la rúbrica. |

F2.2a precede a F2.2b. La conciliación del Excel en F2.2c puede empezar en paralelo. F2.3 puede comenzar con F2.2b para probar la aprobación del primer caso. El cierre completo de F2 también requiere que se satisfagan los casos obligatorios del corte F2.2c.

### Cálculos de referencia

Casos iniciales para acordar en F3.1 y F4.1. Vincular cada resultado a su período, fórmula y unidad. Separar el valor medido del juicio de cumplimiento.

| Indicador | Datos de prueba | Resultado esperado |
|---|---|---|
| Cantidad aditiva | 75 unidades validadas sobre una meta de 100. Al sumar períodos, los registros deben ser distintos y tener la misma unidad. | **75 % de cumplimiento**. Un avance repetido se contabiliza una vez. |
| Proporción | 45 personas distintas capacitadas de 50 elegibles. Meta del 80 %. | **90 % medido** y meta alcanzada. Una jornada sin conteo de personas distintas queda como evidencia, sin modificar el porcentaje. |
| Menor es mejor | Tiempo de respuesta con máximo de 5 días. Probar una medición de 4 y otra de 6. | **4 días cumple** y **6 días incumple**. La comparación usa la unidad y el máximo configurado. |
| Base inválida o ausente | Meta 0 o ausente en una razón avance/meta; población 0 en una proporción; unidad incompatible. | La razón queda sin cálculo y conserva el motivo. Se rechaza la unidad incompatible. |

### Contrato común de evidencias

Acordarlo en F0.1 antes de que F3.2 y F4.2 consuman el servicio. Definir identificador, institución, entidad vinculada, período, autor, tipo, fuente (archivo o URL), acceso, límites, versiones e historial de reemplazos. Ambos módulos comparten los controles de permisos e historial. Se puede preparar el almacenamiento con datos de prueba. Para cerrar el recorrido, probar acceso autorizado, otro rol, otra institución, reintento o reemplazo, y la apertura de la URL o archivo. Una URL de prueba basta para el ensayo de semana 5. La entrega debe verificar todos los formatos que confirme la rúbrica.

### Alineación y aporte estratégico

Con ambos módulos habilitados, registrar opcionalmente la alineación en el **POA en borrador**, contra la versión publicada de un PEI de la misma institución cuya vigencia cubra el año del POA. La aprobación deja la referencia en la versión oficial del POA. Si se agrega o modifica después de publicar, crear una nueva versión para aprobación y preservar la anterior. La demo de F5.2 debe probar captura en borrador, referencia persistida al aprobar y modificación posterior con historial. El modo autónomo deja el vínculo vacío.

Para F5.3, validar correspondencia entre indicador, período, unidad y fórmula antes de incorporar un aporte. Si la actividad aporta evidencia pero le faltan los datos para el cálculo estratégico, conservar la evidencia como tal. Las pruebas cubren avance pendiente o rechazado, unidad incompatible, aporte repetido y un aporte válido rastreable hasta la medición. Una jornada de capacitación aporta al porcentaje únicamente si los datos permiten calcular personas distintas capacitadas sobre la población elegible.

## Seguimiento semanal

Por bloque, registrar responsable, revisor, horas estimadas y reales, caso de aceptación, estado y evidencia en el PR. Medir el avance por **criterios obligatorios aprobados con evidencia respecto del total obligatorio**, indicando los pendientes y bloqueados. Las 21 pruebas Angular y la integración HTTP del incremento son la línea de partida de regresión. Cada semana, volver a estimar lo que queda y ajustar los recortes permitidos antes de comprometer capacidad adicional.
