# SISPLAN Flow · implementación inicial V4

La aplicación institucional real conserva Angular, .NET, PostgreSQL, Keycloak, permisos F0/F1 y contratos existentes. No se incorporan datos ficticios ni módulos del prototipo sin requisitos.

- Cobalto #2563EB, índigo #312E81, celeste #BAE6FD con texto #0C4A6E, lavanda #DDD6FE con texto #3730A3, fondo #E9EEFB. Obsidian #09090B / #18181B / #27272A. Lima no se usa como superficie principal.
- Sidebar flotante contraíble, iconos sólidos, cuenta con icono sólido, cambio de tema animado, preferencia de tema local, foco visible y movimiento reducido.
- Orbit Compass: reconstrucción vectorial aproximada del concepto C3, pendiente de validación de marca final.
- Superficies Bento, Soft Depth, controles Soft Fill, filas con hover, botones con cambios de color sin giros, aviso de guardado flotante.
- Inicio institucional accesible desde el menú, cifras reales de PEI/POA y distribución de estados. El anillo representa proporción de PEI publicados, **no** cumplimiento de metas. No se fabrica una serie mensual: requiere datos de seguimiento F3.
- La navegación inicial, las rutas, la autorización y el acceso Keycloak se mantienen. No se cambia el backend ni la semántica de campos o estados.

## Pendiente de validación y siguientes incrementos

Verificación completa de CI y recorrido manual en ambos temas, 320/390/1280 px, foco, contraste y permisos. Los controles específicos con colores propios requieren una revisión individual. La fuente Inter se usa si está instalada y el fallback actual es DM Sans; no se agregó dependencia. Signature Action se aplica inicialmente a la acción de alta PEI; faltan variantes para todas las acciones. Tabs Liquid Silk, modales, selectores personalizados, gráfico mensual, progreso operativo y tema propio de Keycloak requieren integración posterior conforme al SRS. No se presenta este incremento como aplicación completa ni como despliegue.
