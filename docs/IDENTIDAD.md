# Identidad visual de SISPLAN

**Propuesta para la aplicación académica · 12 de noviembre de 2026**

SISPLAN usa la estructura de un expediente de planificación. Al abrir una actividad, el responsable puede ver a qué objetivo contribuye, qué se programó para el período y qué evidencia respalda lo ejecutado. El diseño conserva esa relación también en los cortes de evaluación.

## Marca

**SISPLAN** · Planificación institucional

El símbolo propuesto es una S escalonada formada por dos piezas angulares y una pestaña lima. La pestaña toma su forma de los índices de un expediente y reaparece en la selección del cuatrimestre y del registro abierto. El nombre acompaña al símbolo en el encabezado. El favicon puede usar el símbolo solo. Preparar una versión monocromática para informes y comprobar su legibilidad a 16 y 24 px antes de incorporarlo al código.

| Uso | Color |
|---|---|
| Encabezado, marca y acción principal | Ciruela `#2C2346` |
| Pestaña activa y foco sobre ciruela | Lima `#D5F36C` |
| Fondo general | Papel `#F6F2E8` |
| Superficie de edición y lectura | Blanco `#FFFFFF` |
| Texto principal / secundario | `#211C2D` / `#5A5066` |
| Borde necesario para identificar un control | `#82788D` |
| Foco sobre papel o blanco | `#4B3278` |

Barlow Condensed para la marca y los títulos (36–42 px en escritorio, 28–32 px en móvil). DM Sans para lectura, controles y tablas (15–16 px para texto habitual). IBM Plex Mono para códigos y versiones (12–13 px). Alinear las cantidades a la derecha, mostrar sus unidades y usar cifras tabulares.

Usar bordes rectos y líneas de separación para agrupar campos y filas. Reservar la lima para indicar la selección. Los resultados se distinguen mediante porcentaje y texto junto al color.

| Resultado | Texto y fondo | Etiqueta |
|---|---|---|
| Desde 90 % | `#215A42` / `#E6F3E9` | Cumplimiento alto |
| Desde 70 % hasta menos de 90 % | `#6A4A00` / `#FFF1CB` | Cumplimiento medio |
| Menor a 70 % | `#8E2F2B` / `#FCEAE7` | Cumplimiento bajo |
| Sin puntuación | `#514765` / `#EFEBF5` | Sin base de cálculo o Pendiente, según la causa |

## Cómo se lee una pantalla

| Elemento | Comportamiento |
|---|---|
| Encabezado del expediente | Muestra título, dependencia y período. En un plan o corte también muestra versión y estado. Ofrece la siguiente acción permitida, como «Registrar avance» o «Revisar evidencia». |
| Margen de trazabilidad | Muestra el registro abierto y sus relaciones autorizadas. Para una actividad, permite seguir PEI, eje, objetivo, POA y producto. Desde el objetivo se abre su indicador. Desde la actividad se abren avances y evidencias. Cada vínculo muestra su código y nombre. |
| Selector de corte | En el POA y la evaluación presenta Enero–abril, Mayo–agosto y Septiembre–diciembre. Al cambiar el corte se actualizan la programación, los avances y la evidencia del ámbito seleccionado. En móvil el selector ocupa el ancho disponible y la ruta se consulta desde «Ver vínculo al PEI». |
| Comparación del corte | Presenta «Plan original», «Plan vigente» y «Ejecutado» para la misma unidad, dependencia y período. El original conserva la primera versión aprobada. El vigente usa la versión aplicable al corte, o la que se fijó al emitir la evaluación. La cifra de ejecución evaluada usa avances validados. |
| Historial | Permite revisar el motivo, la fecha y la autoría de cada cambio. Al revisar una solicitud, presenta original, vigente y propuesta. Un registro de trabajo adicional conserva su origen y separa su autorización, ejecución e incorporación al POA. |

En un PEI en formulación, el margen muestra solo las relaciones estratégicas que existen. La selección de cuatrimestre corresponde a las vistas operativas. Si un usuario abre una evidencia y vuelve, recupera el mismo registro y corte. En móvil, los tres valores de comparación quedan agrupados por actividad para que sus etiquetas sigan a la vista.

## Ejemplo de lectura

**Evaluación cuatrimestral · POA 2026 · Tecnología · Enero–abril**  
Actividad de demostración con unidad «jornadas».

| Plan original | Plan vigente | Ejecutado validado |
|---:|---:|---:|
| 3 | 3 | 2 |

La fila muestra **66,66 % · Cumplimiento bajo**. Al consultar la evidencia se puede abrir el acta asociada al avance validado. Una vez emitido el resultado, el correctivo obligatorio aparece en el mismo expediente. La pantalla distingue la revisión de la evidencia de la emisión y publicación de la evaluación.

Para trabajo adicional, se registra la fecha real y se conserva el origen «Actividad no planificada» aunque se incorpore después. Si ocurrió antes de autorizarse, se indica «Sin autorización previa». Cuando una actividad adicional carece de meta comparable, la cifra muestra su causa según las reglas de cálculo.

## Traslado a Angular

- Aplicar primero la jerarquía y el margen en la lista y el detalle del PEI. Usar las mismas variables de color, tipografía y espaciado en POA, seguimiento y evaluación.
- Construir con encabezados y tablas semánticas, navegación de teclado y etiquetas visibles. Conservar las entradas al corregir errores y anunciar el resultado del guardado.
- Probar el margen desplegable y la comparación a 320 px, ampliación al 200 %, navegación sin ratón y foco visible en ambas superficies. Verificar acceso por rol en las acciones y en los registros vinculados.
- Los pares de texto principal/papel (14,81:1), texto secundario/papel (6,77:1), ciruela/lima (11,75:1) y borde de control/papel (3,74:1) se comprobaron con la fórmula de contraste de WCAG. La prueba de accesibilidad de los componentes corresponde a la implementación.

Las reglas de [operación](plan-sisplan/07-REGLAS-OPERATIVAS.md) y la [trazabilidad del SRS](plan-sisplan/05-TRAZABILIDAD.md) definen los datos y permisos. La lámina ilustra la dirección visual con datos de demostración.

**Antecedente del portal.** Trazario fue la propuesta explorada para el portal del equipo junto con Plantrama y Rumbra. La votación del portal recoge la preferencia del grupo. El [plan base](plan-sisplan/01-PLAN-DE-CONSTRUCCION.md) usa SISPLAN para la entrega académica.
