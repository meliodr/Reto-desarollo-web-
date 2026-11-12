# Plan de frontend Angular — SISPLAN

Fecha de revisión: 12 de noviembre de 2026. Equipo: cinco participantes. Estado: planificación para implementar; no se ha construido ni probado este frontend mediante este documento.

Angular está confirmado por el usuario. El frontend descrito aquí es la **aplicación institucional SISPLAN**, que consumirá la API del producto. El portal académico existente sirve para organizar al equipo y presentar propuestas; sus votos, notas, checklist y simulaciones no son módulos ni datos institucionales de SISPLAN.

## 1. Base verificada y decisiones técnicas

Se contrastó el SRS «Especificación de Requerimientos de Software SISPLAN (2).pdf», versión 1.0, mediante el texto extraído en `tmp/srs_review.txt` y el inventario trazable `tmp/site_requirements.json`. Las referencias de página corresponden a sus 11 páginas. El SRS admite React o Angular y recomienda TailwindCSS y diseño responsivo (§10, p. 10); la selección de Angular procede ahora del usuario. Las decisiones siguientes son propuestas de implementación, salvo esa selección.

| Tema | Propuesta para iniciar | Motivo y condición |
|---|---|---|
| Framework | Angular 22 estable; paquetes Angular y CLI compatibles, con parche exacto fijado al crear el repositorio | La documentación consultada muestra 22.x en soporte activo. No instalar una versión `next` por anticiparse al calendario. [A1] |
| Entorno | Node 24, versión compatible a partir de 24.15.0 dentro de la rama 24; TypeScript `>=6.0 <6.1` administrado mediante la configuración del CLI | Coincide con la matriz de compatibilidad consultada para Angular 22. Fijar versiones y lockfile en CI. [A2] |
| Estructura | Componentes standalone y rutas por dominio, con carga diferida | Reduce dependencias entre los cinco integrantes; agrupa pantalla, lógica y pruebas relacionadas. [A3, A4, A5] |
| Formularios | Reactive Forms estrictamente tipados | Modelo explícito para programación mensual, evidencias y validaciones entre campos. No se necesita adoptar otro sistema de formularios para cumplir el reto. [A6] |
| Estado | Signals para estado local y derivado; HttpClient/RxJS para solicitudes, cancelación y eventos | Un estado por dominio y una sesión común. Una tienda global adicional solo se incorporará si un problema concreto lo justifica. [A7] |
| Presentación | TailwindCSS con tokens institucionales; HTML semántico; CDK cuando un comportamiento complejo lo necesite | Mantener una sola base visual. CDK o Material no certifican por sí mismos la accesibilidad. Integración Tailwind según documentación oficial. [A8, A9] |
| Integración | API `/api/v1`; contratos acordados con backend .NET 10 y PostgreSQL 18 propuestos en este plan | Angular consume contratos; no conoce tablas ni reproduce decisiones de autorización o agregación del servidor. |
| Renderizado | SPA autenticada, inicialmente sin SSR | Es una herramienta de trabajo interna. Añadir SSR, PWA u operación offline requiere una necesidad y un acuerdo adicionales. |
| Calidad | Vitest con utilidades de Angular; pruebas de recorridos con Playwright | El CLI vigente usa Vitest. Las pruebas de navegación pueden usar RouterTestingHarness. [A10, A11, A12] |

Trazario, sus colores y el símbolo de trazo siguen siendo una propuesta de identidad. Configurar tokens de color, tipografía y espaciado permitirá aprobar o cambiar esa identidad sin rehacer cada pantalla. Los nombres oficiales PEI, POA, indicador, producto, actividad y evaluación permanecerán estables.

## 2. Organización por dominios

Propuesta de raíz del frontend: `apps/web`. Cada dominio mantiene sus rutas, pantallas, componentes particulares, cliente API, estado y pruebas cercanos al código que verifican. No crear carpetas globales gigantes separadas únicamente en «components», «services» y «models». Esta organización adapta la guía oficial de Angular al proyecto. [A3]

| Ubicación propuesta | Responsabilidad | Límite |
|---|---|---|
| `src/app/app.config.ts`, `app.routes.ts` | Proveedores y composición de rutas | Sin reglas de PEI, cálculos ni matrices de rol duplicadas |
| `src/app/platform/auth/` | Adaptador OIDC, sesión, salida y guards | No implementa contraseñas ni MFA propio |
| `src/app/platform/http/` | Configuración API, interceptores y errores comunes | No convierte errores en éxitos ni reintenta escrituras indiscriminadamente |
| `src/app/shell/` | Navegación, contexto, cabecera, ayuda y notificaciones | Lee las capacidades permitidas; no las concede |
| `src/app/ui/` | Campo, resumen de errores, diálogo, tabla, estado, archivo y paginación | Componentes presentacionales; sin acceso directo a endpoints de negocio |
| `src/app/administracion/` | Usuarios, dependencias, catálogos y configuración de indicadores | Respetar responsabilidades del Administrador |
| `src/app/pei/` | PEI, ejes, objetivos, resultados, líneas, indicadores y versiones | Expresa relaciones estratégicas sin convertir todo en una jerarquía lineal falsa |
| `src/app/poa/` | POA, productos, actividades, programación y contexto presupuestario | La generación anual y los cambios formales son operaciones del servidor |
| `src/app/cambios-poa/` | Solicitudes de actividades no planificadas y comparación de versiones | Ampliación solicitada por el usuario, con reglas por acordar |
| `src/app/seguimiento/`, `evidencias/` | Avances físicos/financieros, documentos, enlaces e historial | Un registro de ejecución referenciado; evitar doble cómputo |
| `src/app/evaluaciones/` | Tres cuatrimestres, revisión, calificación y correctivos | Mostrar versión y cálculo usados; separar emisión de publicación |
| `src/app/consulta/` | Dashboard, informes y consultas | Cifras oficiales del servidor; no agregar porcentajes arbitrariamente |
| `src/app/proyectos/`, `riesgos/` | Portafolio/programa/proyecto; riesgos y mitigación | Alcance del §1.3, sin RF numerados específicos; definición funcional pendiente |
| `src/app/auditoria/` | Consulta de cambios autorizada | La auditoría se registra en servidor, no en eventos locales del navegador |

Dependencia permitida: pantalla → estado/servicio de su dominio → cliente API. Los dominios comparten controles de `ui` y contratos publicados; no importan componentes internos de otros dominios para saltarse sus límites. Un cambio de contrato debe actualizar consumidor, prueba y documentación en la misma entrega coordinada.

Estado compartido mínimo: usuario y capacidades, dependencias accesibles, PEI/POA seleccionados y notificaciones. Los filtros compartibles van en query parameters. Formularios en edición y tablas permanecen en su dominio. Al cerrar sesión o cambiar de dependencia se invalida la información que corresponda al contexto anterior.

## 3. Rutas y pantallas propuestas

Estas son **rutas de interfaz**, no una especificación de endpoints. `:id` identifica un recurso; elegir un identificador en la URL no concede acceso. El servidor verifica la dependencia y el alcance en cada operación.

| Ruta | Pantalla y acciones principales | Base de alcance |
|---|---|---|
| `/acceso`, `/auth/callback` | Entrada institucional, retorno OIDC y estado de sesión | RNF-001; §10 |
| `/sin-acceso`, ruta desconocida | Explicación de acceso denegado o recurso no disponible; regreso seguro | Propuesta transversal |
| `/inicio` | Bandeja según capacidades: trabajo pendiente, evaluaciones, observaciones o consulta ejecutiva | RF-018 y §3; diseño propuesto |
| `/administracion/usuarios`, `/dependencias`, `/catalogos` bajo `/administracion` | Listas, alta/edición e inactivación permitida; membresías y alcance | §3.1 y §11; detalles por acordar |
| `/pei`, `/pei/nuevo`, `/pei/:peiId` | Lista; ficha; formulación; estado, vigencia y versión | RF-001, CU-001 |
| `/pei/:peiId/ejes`, `/objetivos`, `/indicadores` bajo el mismo PEI | Gestión vinculada; resultados y líneas de acción en su contexto | RF-002–004; §1.3 |
| `/pei/:peiId/mapa`, `/versiones` bajo el mismo PEI | Relaciones estratégicas con alternativa textual; historial y comparación | §1.3; RF-002 |
| `/poa`, `/poa/:poaId` | POA anual, resumen y vínculo al PEI; resultado de generación anual | RF-005, RN-001 |
| `/poa/:poaId/productos`, `/productos/:productoId` bajo el mismo POA | Producto, responsable y vínculo eje–objetivo–indicador | RF-006–007 |
| `/poa/:poaId/actividades`, `/actividades/:actividadId` bajo el mismo POA | Actividades, cronograma, responsables y detalle trazable | RF-008, RN-002 |
| `/poa/:poaId/programacion` | Programación anual, mensual y cuatrimestral; unidades y presupuesto pertinente | RF-009; alcance §1.3 |
| `/poa/:poaId/actividades/:actividadId/avances/nuevo` | Registro físico/financiero, comentario y evidencia | RF-010, CU-002 |
| `/poa/:poaId/cambios`, `/cambios/nuevo`, `/cambios/:solicitudId` bajo el mismo POA | Registrar, justificar, revisar e incorporar trabajo no planificado | Ampliación del usuario; casos `PROP-CU-NP-*` |
| `/evidencias/:evidenciaId` | Metadatos, descarga/consulta y versiones; enlaces a registros relacionados | RF-011; gestión documental §1.3 |
| `/poa/:poaId/evaluaciones`, `/evaluaciones/:evaluacionId` bajo el mismo POA | Tres evaluaciones generadas; bandeja y revisión contextual | RF-012–014; CU-003 |
| `/planes-correctivos`, `/planes-correctivos/:planId` | Plan requerido por incumplimiento, seguimiento y resultado | RF-015, RN-008 |
| `/dashboard` | PEI/POA vigentes y cumplimiento institucional, por eje, área e indicador | RF-016 |
| `/reportes` | PEI, POA, cuatrimestral, anual, indicadores y presupuestario; PDF/Excel | RF-017 y §10 |
| `/notificaciones` | Evaluaciones pendientes, vencimientos, indicadores sin actualizar e incumplimientos | RF-018, RN-007 |
| `/proyectos`, `/proyectos/:proyectoId` | Portafolios, programas, cronograma, hitos y entregables en vistas relacionadas | Alcance §1.3; comportamiento por precisar |
| `/riesgos`, `/riesgos/:riesgoId` | Riesgo estratégico/operativo, probabilidad–impacto y mitigación | Alcance §1.3; escalas por precisar |
| `/auditoria` | Filtro por recurso, usuario y fecha; antes/después del cambio | RNF-002, §11; rol lector pendiente |

Cada ficha usa una cabecera con nombre, código, dependencia, período, estado y versión. Los enlaces de contexto permiten recorrer actividad → producto → objetivo/eje → PEI sin perder el registro actual. La navegación móvil conserva las acciones principales; los formularios completos se abren como páginas, no como una secuencia de modales pequeños.

## 4. Los tres recorridos oficiales

Se conservan los identificadores y la secuencia del SRS (§12, p. 11). Las pantallas sugeridas no amplían por sí mismas los permisos.

| Caso oficial | Secuencia del SRS | Traducción a interfaz y cautelas |
|---|---|---|
| **CU-001 Crear PEI** — Planificador | Crear PEI → Registrar ejes → Registrar objetivos → Registrar indicadores → Aprobar → Publicar | Formulación por secciones con borradores y revisión final. El SRS incluye aprobación/publicación en el flujo, pero §3 no resuelve inequívocamente qué rol ejerce ambas acciones sobre PEI; cerrar la decisión antes de habilitarlas. |
| **CU-002 Registrar Avance** — Responsable de Área | Seleccionar actividad → Registrar avance → Adjuntar evidencia → Guardar | Abrir desde la bandeja o actividad; mostrar período, unidad y programación; conservar valores cuando falle la carga; guardar con confirmación de API y enlace al registro. |
| **CU-003 Evaluar Cumplimiento** — Evaluador | Verificar evidencias → Validar avance → Asignar calificación → Emitir resultado | Vista de programación, ejecución y evidencia comparables. Indicar regla, denominador y versión. Publicar evaluación es una acción distinta atribuida al Administrador por §3.1. |

La demo integrada seguirá PEI → POA anual → producto y actividad → programación → avance con evidencia → evaluación → correctivo cuando corresponda → dashboard e informe. El trabajo no planificado se demostrará como variante trazable de ese recorrido, conservando su origen.

## 5. Formularios, reglas y recuperación

Los campos del SRS son la base. Límites de longitud, monedas, catálogos, estados y obligatoriedad adicional deben venir del contrato aprobado, no de criterios distintos en cada formulario. Reactive Forms aporta validación local tipada; la API vuelve a validar lo recibido. Los campos deshabilitados no aparecen normalmente en `form.value`: construir el DTO de escritura explícitamente y nunca reenviar indiscriminadamente todos los valores de un formulario. [A6]

| Formulario | Contenido y comportamiento | Decisión que debe resolverse |
|---|---|---|
| PEI | Nombre, período, inicio/fin, misión, visión, valores y estado; secciones con resumen final | Duración ordinaria de 4 años/extraordinaria de 5 y semántica inclusiva del ejemplo 2026–2030; estados y aprobación |
| Ejes/objetivos | Código/nombre/descripción; resultado y línea de acción; asociación estratégica visible | Cardinalidades definitivas y quién puede inactivar/versionar; no eliminar registros aprobados, RN-0098 |
| Indicador | Nombre, fórmula, unidad, línea base, meta y frecuencia; explicación legible de variables | Catálogo de fórmulas, polaridad, precisión y agregación; nunca ejecutar una fórmula introducida como JavaScript |
| Producto/actividad | Producto, unidad, responsable y vínculos; actividad con cronograma y responsable | Herencia de vínculos y ubicación exacta de metas/presupuesto; selector de responsables limitado al ámbito autorizado |
| Programación | Año y 12 meses con subtotales cuatrimestrales; mostrar unidad y tipo de meta | Sumar meses solo para magnitudes aditivas; porcentajes, hitos y metas acumuladas necesitan su propia regla |
| Avance | Período, físico, financiero, comentario, evidencia y fecha real | Distinguir valor del período de acumulado; RN-006 limita avance al 100 %, pero tratamiento de sobreejecución física debe aclararse; no recortar silenciosamente valores |
| Evidencia | PDF, Word, Excel, imágenes o enlace; descripción, versión y registro al que demuestra | Extensiones/MIME, tamaños, seguridad de archivos y conservación; `accept` ayuda a elegir, no valida seguridad |
| Evaluación | Programado, ejecutado, porcentaje, semáforo, evidencia y observaciones | División por cero, redondeo y decimales entre 89 y 90; agregación institucional y relación con RN-006 |
| Correctivo | Incumplimiento que lo origina, responsable, acciones, fechas y evidencia de cierre | Estos campos detallan una propuesta; el SRS exige el plan por debajo del 70 %, sin especificar todo su formulario |

No interpretar «sin dato» como cero, ni «sin programación» como 100 %. Mantener monto y moneda separados, cantidades y unidades visibles, fechas de calendario sin conversiones horarias accidentales y marcas de auditoría con zona definida. La API será la autoridad para cálculos y fechas efectivas; la UI puede anticipar una validación, pero muestra el resultado confirmado.

El contrato común propone cantidades decimales e importes como cadenas decimales. El frontend conserva esa precisión al construir el DTO, valida formato/escala y aplica formato localizado solo para presentación; no transforma importes en `Number` para recalcular totales financieros oficiales. Las respuestas de edición conservan `ETag`, que se envía como `If-Match` al actualizar.

Patrón de trabajo para formularios extensos:

1. Agrupar por contexto comprensible: identificación, relaciones, programación, evidencia y revisión. Mostrar secciones pendientes sin borrar las completadas.
2. Ofrecer **Guardar borrador** si el dominio admite borradores persistidos. «Cambios sin guardar», «Guardando», «Guardado» y «Error al guardar» corresponden a estados reales; únicamente la respuesta confirmada habilita el mensaje de guardado.
3. Al enviar, mostrar resumen de errores con enlaces a campos, mensajes junto a cada campo y valores conservados. El foco va al resumen; los errores de negocio no asociados a un campo permanecen visibles. Se adapta el patrón GOV.UK, no su identidad visual. [D1]
4. Antes de aprobar, publicar o incorporar, presentar resumen de lo que cambiará y posibilidad de volver a editar. Evitar confirmaciones repetitivas en cambios ordinarios. [D2]
5. Usar `CanDeactivate` como aviso de navegación con cambios pendientes. No considerarlo garantía ante cierre abrupto del navegador: la recuperación real necesita borrador persistido. [A13]

Los archivos muestran estados independientes: seleccionado, cargando, recibido, disponible o rechazado. Un fallo no debe perder el avance introducido. La vista integrada de PDF/imagen es conveniente; abrir Word/Excel en un editor dentro del navegador no es requisito del SRS. Las descargas requieren autorización, y los enlaces se validan sin insertar HTML arbitrario.

## 6. Actividades no planificadas

Origen: nueva indicación del usuario, no flujo aprobado previamente por el maestro. Utilizar IDs de propuesta `PROP-*` y conservar los RF originales. Los campos y permisos deberán contrastarse antes de cerrar la implementación.

| Paso de interfaz | Información y decisión | Efecto permitido |
|---|---|---|
| Detectar y registrar | Necesidad, origen, dependencia, fecha de detección, responsable propuesto y estado real de ejecución | Crear borrador de solicitud con identidad propia |
| Justificar | Aporte, urgencia, vínculos, meta/unidad, fechas, costo/fuente e impacto sobre compromisos vigentes | Preparar una propuesta comparable; no alterar el POA |
| Revisar | Comparar original, vigente y propuesta; aprobar, devolver o rechazar con motivo y actor | Registrar decisión según permisos acordados |
| Incorporar | Mostrar nueva versión, fecha efectiva y referencia a la aprobación | Crear versión formal del POA; la línea base original permanece |
| Dar seguimiento | Avances, evidencias, evaluación y reporte con versión y período explícitos | Referenciar ejecución una sola vez y preservar el origen no planificado |

Presentar dos dimensiones separadas: **autorización** —borrador, en revisión, devuelta, rechazada, aprobada pendiente de incorporación, incorporada— y **ejecución** —no iniciada, en ejecución, completada—. Son estados propuestos. Registrar trabajo ya realizado conserva sus fechas reales y no fabrica una aprobación previa. Aprobar una solicitud tampoco equivale a aprobar gasto ni a incorporarla automáticamente al POA.

La pantalla no aumenta porcentajes por añadir trabajo extra, ni rehace denominadores de evaluaciones cerradas. Deben acordarse rol aprobador, separación solicitante/decisor, autoridad presupuestaria, urgencias, fecha efectiva y contribución a indicadores.

## 7. Sesión, capacidades y permisos visibles

Propuesta común con backend: SPA OIDC Authorization Code con PKCE y MFA en el proveedor de identidad. Una biblioteca OIDC compatible administra el protocolo y access token en memoria; no incluir secretos de cliente ni implementar criptografía o renovación manualmente. Proveedor y biblioteca exactos se seleccionan en la fase de preparación. La aplicación consumirá `GET /api/v1/me` para conocer usuario, dependencias, roles y capacidades orientativas de interfaz. La API siempre vuelve a decidir por operación y recurso.

| Rol del SRS | Acciones que la interfaz debe contemplar, sujetas al alcance del recurso | No deducir automáticamente |
|---|---|---|
| Administrador | Catálogos, configuración de indicadores, usuarios y publicación de evaluaciones | Acceso universal a todo, aprobación de PEI o aprobación de gasto |
| Planificador Institucional | Crear/formular PEI, crear POA, aprobar productos, consolidar avances e informes institucionales | Que también publique evaluaciones o autorice toda modificación |
| Responsable de Área | Crear actividades, registrar avances, adjuntar evidencias y actualizar indicadores | Editar otras dependencias o aprobar su solicitud |
| Evaluador | Revisar evidencias, validar avances y calificar cumplimiento | Publicar evaluaciones o administrar usuarios |
| Consultor | Consulta, dashboard y descarga de reportes | Escritura o administración |
| Consulta Ejecutiva | Dashboard | Descarga de reportes ni acceso al detalle sensible sin decisión expresa |

Usar guards funcionales de sesión/capacidad para orientar la navegación y `CanDeactivate` para cambios pendientes. Una capacidad general permite mostrar una sección; acciones de un registro además dependen de su estado, ámbito y operaciones permitidas por la respuesta del servidor. Un botón oculto, deshabilitado o una ruta bloqueada no protege una API. Angular lo advierte expresamente. [A13]

No mostrar fugazmente contenido restringido mientras carga `/me`. En edición aprobada, explicar el estado bloqueado y ofrecer una operación de versión cuando proceda. No basar los permisos únicamente en comparar nombres de roles dispersos en componentes. Los seis roles del producto son distintos de los cinco miembros que desarrollan el proyecto.

Interceptores funcionales centralizan cabecera bearer exclusivamente hacia la API prevista, correlación y manejo de errores; nunca enviar tokens a enlaces o almacenes externos de evidencias. Los interceptores funcionales son la opción recomendada por Angular. [A14]

| Respuesta/estado | Comportamiento UX acordado con backend |
|---|---|
| Cargando / sin resultados | Mostrar estado explícito y filtros activos; una lista vacía no aparenta fallo |
| `401` | Reautenticar mediante la biblioteca; impedir bucles y avisar si hay cambios sin guardar. La navegación al IdP puede perder memoria local: el borrador persistido es la recuperación fiable |
| `403` | Explicar falta de permiso y ofrecer volver; no reintentar como si fuera sesión expirada |
| `404` | Recurso no disponible; no revelar datos de otros ámbitos |
| `409` | Regla de estado/transición incompatible: recargar estado y explicar qué acción cambió |
| `412` | `If-Match` obsoleto: comparar datos propios y versión vigente, conservar edición y pedir resolver; no sobrescribir automáticamente |
| `422` | Mapear Problem Details/errores del contrato a campos y resumen; mantener entradas |
| Red o `5xx` | Avisar de fallo; permitir recuperación. Si una escritura quedó incierta, consultar su resultado antes de repetirla |

Los documentos, comentarios y fórmulas se presentan como datos. Usar las protecciones de plantillas Angular; evitar bypass de sanitización y HTML dinámico no confiable. CSP y Trusted Types se coordinan con infraestructura. [A15]

## 8. Reglas de diseño y accesibilidad verificables

Objetivo propuesto: WCAG 2.2 nivel AA en los recorridos del producto. Es un objetivo de calidad del equipo, no una certificación ni una obligación legal atribuida aquí a la institución. WCAG contiene criterios normativos; sus páginas Understanding explican su aplicación. GOV.UK y USWDS aportan patrones adaptables, no una identidad que SISPLAN deba copiar.

| Criterio | Regla aplicable y comprobación |
|---|---|
| Contraste textual — 1.4.3 AA | 4.5:1 texto normal y 3:1 texto grande; verificar los pares reales, incluidos mensajes y controles. [W1] |
| Contraste no textual — 1.4.11 AA | 3:1 en información visual necesaria para identificar controles, estados y gráficos, con las excepciones del criterio. [W2] |
| Color — 1.4.1 A | Semáforo con porcentaje y etiqueta; color nunca como única señal. [W0] |
| Teclado — 2.1.1 y 2.1.2 A | Completar formularios, revisar evidencia y salir de diálogos sin ratón ni trampas. [W0] |
| Foco — 2.4.7 AA y 2.4.11 AA | Foco visible; cabeceras/barras no ocultan por completo el control enfocado. Buscar visibilidad completa como mejora. [W0, W3] |
| Objetivo táctil — 2.5.8 AA | Mínimo 24 × 24 CSS px o excepción válida. Preferir 44 × 44 en acciones principales; 44 corresponde al criterio 2.5.5 AAA, no al mínimo AA. [W4] |
| Reflujo — 1.4.10 AA | Formularios y navegación utilizables a 320 CSS px. La tabla bidimensional puede desplazarse horizontalmente en su contenedor; la página y las celdas deben conservar legibilidad. [W5] |
| Semántica — 1.3.1 A | Encabezados, labels, fieldsets, caption y encabezados de tabla relacionados; ordenar mediante botón y anunciar orden. [W0, D3] |
| Errores — 3.3.1 A, 3.3.2 A, 3.3.3 AA | Identificar campo, describir error y ayuda de corrección; conservar valores. [W6, D1] |
| Cambios dinámicos — 4.1.3 AA | Anunciar guardado, carga terminada y errores sin mover foco innecesariamente. [W0] |
| Autenticación — 3.3.8 AA | Permitir gestores y pegado, también en OTP cuando aplique; verificar accesibilidad del IdP/MFA seleccionado. [W7] |
| Navegación — 2.4.1 A y 2.4.2 A | Enlace para saltar contenido, título por ruta y encabezado principal. Tras navegar, gestionar foco en destino. [W0, A9] |

Tablas de POA: filtros etiquetados, encabezados claros, unidad en columna/celda, paginación de servidor y selección que no se pierda sin aviso. Acompañar gráficos con tabla o resumen equivalente. El mapa estratégico requiere un recorrido textual por relaciones. No usar arrastrar como único método para ordenar o programar. Respetar movimiento reducido en transiciones.

## 9. Dependencias y orden de construcción

No esperar a «terminar todo el backend» para empezar la interfaz, ni declarar integración terminada mientras responde un mock. Cada dominio puede avanzar con contratos y ejemplos marcados, y debe conectar una operación real tempranamente.

| Tramo | Depende de | Entregable revisable |
|---|---|---|
| 1. Base común | Versiones, contrato `/me`, configuración OIDC y convenciones API | Shell, acceso, selección de contexto, errores, controles, ruta protegida y compilación CI |
| 2. PEI y catálogos | Identificadores, vigencia, relaciones y permisos mínimos | Crear/formular un PEI persistido; consulta y edición de sus elementos |
| 3. POA | PEI válido, contrato de generación y producto/actividad | POA real por año, vínculos, responsables y programación sin doble generación |
| 4. Avance y evidencia | Actividad/programación, período y contrato de carga | CU-002 completo contra API con fallo de archivo recuperable |
| 5. Evaluación/correctivo | Ejecución/evidencia; fórmula y estados acordados | CU-003, tres períodos, revisión y publicación separadas; correctivo bajo umbral |
| 6. Consulta y cambios | Agregaciones, exportación y versionamiento | Dashboard e informes consistentes; solicitud no planificada y versión preservada |
| 7. Alcance complementario y cierre | Definición de proyectos/riesgos; auditoría y alertas | Pantallas de esos dominios, recorrido final y evidencia de aceptación |

Los tramos no autorizan omitir proyectos o riesgos: aparecen en el alcance original aunque no tengan RF numerado. Pueden trabajarse en paralelo al acordar sus contratos. El calendario global debe reservar integración y corrección antes de la entrega de la semana 6.

Cada participante desarrolla la interfaz de su dominio junto con API, datos y verificación de su entrega. Una persona coordina convenciones visuales y contratos de frontend, pero no concentra toda la programación Angular. Otro integrante revisa cada flujo; los cambios en controles comunes se prueban con al menos dos pantallas consumidoras relevantes.

## 10. Criterios de aceptación del frontend

Una pantalla dibujada, una simulación navegable o una compilación correcta no prueban que un requisito esté cumplido. Para marcarlo terminado se necesita persistencia/integración real, resultado verificable y evidencia identificada por versión.

| Área | Evidencia prevista |
|---|---|
| Base Angular | Instalación reproducible desde lockfile, build de producción, comprobación de tipos/plantillas y pruebas relevantes en CI; cargas diferidas verificadas |
| Rutas y permisos | Entrada por URL directa, usuario sin sesión, rol sin capacidad y recurso de otra dependencia; rechazo de API aunque se manipule la interfaz |
| Formularios | Requeridos, cruces de fechas/unidades, valores nulos/cero, errores de API y resumen accesible; prueba del borrador y navegación con cambios |
| Concurrencia | Dos sesiones editan un registro; la segunda recibe conflicto de versión y resuelve sin pérdida silenciosa |
| PEI/POA | CU-001 y generación anual; relaciones válidas, responsable obligatorio y protección de aprobados |
| Seguimiento | CU-002; archivo que falla, reintento controlado, avance persistido y versión de evidencia visible |
| Evaluación | CU-003; 0 programado, límites 70/90 y decimales según decisión; evidencia requerida, correctivo bajo 70 y publicación autorizada |
| No planificado | Rechazo/devolución conservan historia; ejecución previa no obtiene aprobación retroactiva; incorporación crea versión sin doble ejecución |
| Consulta | Dashboard, tabla e informe concuerdan para el mismo filtro, período y versión; PDF/Excel descargables y legibles |
| Accesibilidad | Recorrido manual con teclado y lector de pantalla, zoom/reflujo, contraste y comprobación automática; no reducir revisión a puntuación de herramienta |
| Estados y errores | Carga, vacío, guardado fallido, sesión expirada y respuesta incierta; ningún falso mensaje de éxito |
| Alcance adicional | Criterios de proyectos/riesgos trazados a §1.3, una vez definidos; no inventar RF oficiales |

Pruebas de unidad/componente para reglas de presentación, validación y manejo de estados; pruebas de servicios HTTP con respuestas y errores pactados; navegación con RouterTestingHarness; E2E de los tres CU y del flujo no planificado. Las pruebas de autorización del servidor pertenecen además a backend y no se sustituyen por probar guards. No usar un porcentaje de cobertura de líneas como porcentaje de cumplimiento del SRS.

## 11. Decisiones por cerrar antes de programar su comportamiento

1. Identidad y biblioteca de componentes; la selección de Angular ya está resuelta.
2. Proveedor OIDC, biblioteca cliente, MFA y recuperación de sesión.
3. Matriz de capacidades por recurso/dependencia; aprobadores y publicadores del PEI; acceso a auditoría.
4. Fechas y estados del ciclo; edición/versionamiento de aprobados y generación anual.
5. Fórmulas, meta cero, redondeo, sobreejecución, semáforo decimal y agregaciones.
6. Ubicación de programación/presupuesto y semántica período/acumulado.
7. Reglas, permisos y efectos de trabajo no planificado.
8. Tipos/tamaños/retención de evidencia y protocolo de carga.
9. Alcance verificable de proyectos y riesgos; cronogramas, escalas y permisos.
10. Contratos de informes, filtros, moneda y zona horaria institucional.

## 12. Fuentes consultadas

Fuentes oficiales verificadas el 12/11/2026. La selección de arquitectura y las rutas son propuestas del equipo fundamentadas en ellas, no requisitos impuestos por Angular, GOV.UK o USWDS.

- **SRS** — «Especificación de Requerimientos de Software SISPLAN (2).pdf», v1.0: §1.3 alcance (pp. 1–2), §2 ciclos (pp. 2–3), §3 roles (pp. 3–4), §5 RF (pp. 5–8), §6–7 reglas/seguridad/auditoría (pp. 8–9), §10 arquitectura (p. 10), §12 CU (p. 11). Fuente aportada por el usuario.
- **A1** — [Angular: versiones y soporte](https://angular.dev/reference/releases).
- **A2** — [Angular: compatibilidad de versiones](https://angular.dev/reference/versions).
- **A3** — [Angular: guía de estilo y organización por funcionalidades](https://angular.dev/style-guide).
- **A4** — [Angular: componentes](https://angular.dev/guide/components).
- **A5** — [Angular: estrategias de carga de rutas](https://angular.dev/best-practices/performance/lazy-loaded-routes).
- **A6** — [Angular: Reactive Forms tipados](https://angular.dev/guide/forms/typed-forms) y [Reactive Forms](https://angular.dev/guide/forms/reactive-forms).
- **A7** — [Angular: Signals](https://angular.dev/guide/signals).
- **A8** — [Angular: Tailwind](https://angular.dev/guide/tailwind).
- **A9** — [Angular: accesibilidad](https://angular.dev/best-practices/a11y).
- **A10** — [Angular: pruebas](https://angular.dev/guide/testing).
- **A11** — [Angular: pruebas de navegación](https://angular.dev/guide/routing/testing).
- **A12** — [Playwright: documentación de instalación y capacidades](https://playwright.dev/docs/intro).
- **A13** — [Angular: guards y autorización del servidor](https://angular.dev/guide/routing/route-guards).
- **A14** — [Angular: interceptores HTTP](https://angular.dev/guide/http/interceptors).
- **A15** — [Angular: seguridad de la aplicación](https://angular.dev/best-practices/security).
- **D1** — [GOV.UK: resumen de errores](https://design-system.service.gov.uk/components/error-summary/).
- **D2** — [GOV.UK: revisión antes de enviar](https://design-system.service.gov.uk/patterns/check-answers/).
- **D3** — [USWDS: tablas](https://designsystem.digital.gov/components/table/).
- **W0** — [W3C: WCAG 2.2, recomendación normativa](https://www.w3.org/TR/WCAG22/).
- **W1** — [W3C: contraste mínimo](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html).
- **W2** — [W3C: contraste no textual](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html).
- **W3** — [W3C: foco no oculto, mínimo](https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html).
- **W4** — [W3C: tamaño mínimo de objetivos](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html).
- **W5** — [W3C: reflujo](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html).
- **W6** — [W3C: identificación de errores](https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html).
- **W7** — [W3C: autenticación accesible](https://www.w3.org/WAI/WCAG22/Understanding/accessible-authentication-minimum.html).
