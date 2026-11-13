# Reto Web PEI-POA

Proyecto académico de INTEC para un equipo de **cinco integrantes**.

El sistema debe gestionar la planificación estratégica y operativa de una institución y relacionar los objetivos del PEI con los productos, las actividades, los avances y las evidencias del POA. La aplicación se entrega con el nombre **SISPLAN**, que utiliza el SRS. La dirección visual propuesta está en la guía de identidad.

[Portal del proyecto](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/)

## Requisitos y fuentes

La consigna y la **Especificación de Requerimientos de Software SISPLAN** definen el alcance original. Los archivos **Ejes y Objetivos DIGEMAPS (1).xlsx** y **POA TI 2026 V2.xlsx** sirven como referencias para los datos estratégicos y la programación operativa.

El análisis identifica 18 requisitos funcionales, 7 reglas de negocio, 2 requisitos no funcionales y 3 casos de uso oficiales. El portal presenta también 9 casos de uso propuestos, separados de los originales.

## Proceso principal

1. Formular el PEI con sus ejes, objetivos, resultados, indicadores y metas.
2. Generar un POA por cada año del PEI.
3. Registrar productos y actividades con responsables, programación y presupuesto.
4. Registrar avances físicos y financieros y adjuntar evidencias.
5. Evaluar el cumplimiento en tres cuatrimestres.
6. Elaborar planes correctivos cuando el cumplimiento sea menor al 70 %.
7. Consultar resultados y generar reportes con auditoría de los cambios.

Cada producto debe mantener su relación con el eje, el objetivo y el indicador estratégico al que contribuye.

## Ampliación solicitada

Se ha solicitado incorporar el registro de **actividades no planificadas**. Esta ampliación se documenta aparte del SRS original. El registro, la autorización y el tratamiento de metas y presupuesto siguen las [reglas operativas de la base](docs/plan-sisplan/07-REGLAS-OPERATIVAS.md#trabajo-no-planificado-proyectos-riesgos-y-alertas).

## Estado

El portal de trabajo v2 está publicado. Permite asignar tareas, completar checklists, compartir notas, crear encuestas y revisar entregables entre integrantes. Su lectura pública se comprobó con 29 tareas y cinco plazas disponibles.

En el entorno de desarrollo se aprobaron **21 pruebas del portal**: 17 del servicio del tablero y 4 regresiones de interfaz. La incorporación y los recorridos con las cinco cuentas reales siguen pendientes.

El plan base v1.0 fija el alcance, las reglas operativas, el reparto y las herramientas. El equipo eligió **Angular**. La base técnica usa .NET 10, PostgreSQL 18 y Keycloak con MFA. El recorrido de formulación, publicación del PEI y generación de POA está en [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1), en revisión.

La fecha de cierre visible en la consigna es el **2 de diciembre de 2026 a las 23:59**. El objetivo de trabajo referido a la **semana 6** está pendiente de conciliar con ese plazo; todavía no se le asigna una fecha.

## Documentación

- [Arranque local y pruebas de la aplicación](docs/plan-sisplan/06-PRIMER-INCREMENTO.md)
- [QA del recorrido PEI–POA](docs/plan-sisplan/09-QA-PEI-POA.md)

- [Plan base v1.0 con reglas, herramientas, responsables y entregables](docs/plan-sisplan/01-PLAN-DE-CONSTRUCCION.md)
- [Verificación del SRS, los Excel y la consigna](docs/plan-sisplan/05-TRAZABILIDAD.md#verificación-del-srs-y-los-excel)
- [Estado del proyecto](docs/ESTADO.md)
- [Método de trabajo](docs/METODO-DE-TRABAJO.md)
- [Decisiones y asuntos pendientes](docs/DECISIONES.md)
- [Uso del portal y participación del equipo](docs/PORTAL.md)
- [Identidad visual de SISPLAN](docs/IDENTIDAD.md)
- [Actividades no planificadas](docs/AMPLIACION-NO-PLANIFICADA.md)
- [Referencia del reto anterior](docs/REFERENCIA-RETO-ANTERIOR.md)
