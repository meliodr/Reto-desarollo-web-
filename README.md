# SISPLAN — Reto Web PEI-POA

Proyecto académico del Instituto Tecnológico de Santo Domingo (INTEC), organizado para un equipo de cuatro integrantes.

SISPLAN tiene como objetivo gestionar el ciclo de planificación estratégica y operativa de una institución y conservar la trazabilidad entre sus objetivos, productos, actividades, ejecución y evaluación.

[Portal del proyecto](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site)

## Alcance y autoridad de los requisitos

La fuente de requisitos es la **Especificación de Requerimientos de Software SISPLAN**, facilitada por el docente, junto con la consigna del reto. Los archivos **Ejes y Objetivos DIGEMAPS (1).xlsx** y **POA TI 2026 V2.xlsx** son referencias del modelo estratégico y operativo.

Los requisitos confirmados deben distinguirse de las decisiones del equipo, las propuestas de implementación y las dudas pendientes de resolver. Una propuesta no modifica por sí sola el SRS. Los documentos fuente todavía no se encuentran incorporados en este repositorio.

El repositorio de [Tendencias en Desarrollo de Software / FuelTrack](https://github.com/Rowlistp/ING-IDS347-01-TENDENCIAS-EN-DESARROLLO-DE-SOFTWARE) sirve como referencia de organización, revisión y evidencia. Sus requisitos de negocio no se trasladan a SISPLAN.

## Flujo funcional que se busca desarrollar

1. Formular el PEI y sus ejes, objetivos, indicadores y metas.
2. Generar los POA de los años comprendidos en el PEI.
3. Definir productos y actividades con responsables, programación y presupuesto.
4. Registrar avances físicos y financieros y adjuntar evidencias.
5. Evaluar el cumplimiento en tres cuatrimestres del año.
6. Solicitar un plan correctivo cuando el cumplimiento sea menor al 70 %.
7. Consolidar resultados y producir reportes con trazabilidad y auditoría.

La relación que debe poder seguirse es:

**PEI → POA → actividad → avance/evidencia → evaluación → correctivo, si corresponde → reporte.**

Los productos operativos deben conservar además su vínculo con el eje, el objetivo y el indicador estratégico.

## Estado de la preparación

La implementación funcional de SISPLAN está **SIN INICIAR**. No existen todavía código de aplicación, pruebas ejecutadas ni despliegue acreditado del sistema institucional. La documentación y el portal del proyecto no equivalen a funcionalidad implementada.

La arquitectura y las tecnologías definitivas están pendientes de decisión. Este repositorio no fija un stack por anticipado.

- [Estado y evidencia](docs/ESTADO.md)
- [Método de trabajo](docs/METODO-DE-TRABAJO.md)

## Forma de colaboración

Se trabajará mediante cambios acotados, ramas por tarea, pull requests y revisión. Cada requisito implementado deberá vincularse con su cambio, su verificación y la evidencia disponible. Los resultados de pruebas se comunicarán con fecha, entorno y límites; no se declarará cumplimiento total sin respaldo.
