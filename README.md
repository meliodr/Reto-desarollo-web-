# Reto Web PEI-POA

Proyecto académico de INTEC para un equipo de **cinco integrantes**.

**SISPLAN** permite trabajar con el PEI y el POA por separado o vincularlos cuando una institución usa ambos módulos. El [SRS v2 y su adaptación](docs/plan-sisplan/10-ADAPTACION-SRS-V2.md) orientan el alcance del reto.

[Portal del proyecto](https://sisplan-reto-web-equipo.bzps4s5pp8.chatgpt.site/)

## Requisitos y fuentes

La consigna y el **SRS v2 del Reto PEI y POA (noviembre–enero 2027)** definen el alcance vigente. Los archivos **Ejes y Objetivos DIGEMAPS (1).xlsx** y **POA TI 2026 V2.xlsx** sirven como referencias para los datos estratégicos y la programación operativa.

La [matriz anterior](docs/plan-sisplan/05-TRAZABILIDAD.md) documenta los requisitos numerados de la v1.

## Proceso principal

1. Cargar la estructura estratégica y preparar el PEI.
2. Enviar a revisión, registrar observaciones y corregir. El director revisa, el comité valida y la máxima autoridad aprueba.
3. Formular un POA anual independiente o vincularlo con un PEI publicado. Se puede dejar vacío el presupuesto o indicar un monto de referencia en RD$.
4. En el módulo operativo, programar productos y actividades, registrar avances y evidencias y evaluar resultados.

## Ampliación solicitada

Se ha solicitado incorporar el registro de **actividades no planificadas**. Esta ampliación se documenta aparte del SRS original. El registro, la autorización y el tratamiento de metas y presupuesto siguen las [reglas operativas de la base](docs/plan-sisplan/07-REGLAS-OPERATIVAS.md#trabajo-no-planificado-proyectos-riesgos-y-alertas).

## Estado

El portal de trabajo v2 está publicado. Permite asignar tareas, completar checklists, compartir notas, crear encuestas y revisar entregables entre integrantes. Su lectura pública se comprobó con 29 tareas y cinco plazas disponibles.

En el entorno de desarrollo se aprobaron **21 pruebas del portal**: 17 del servicio del tablero y 4 regresiones de interfaz. La incorporación y los recorridos con las cinco cuentas reales siguen pendientes.

El [PR #1](https://github.com/meliodr/Reto-desarollo-web-/pull/1) incorpora la carga DIGEMAPS, el nuevo flujo de aprobación del PEI y la creación de POA en ambas modalidades con presupuesto de referencia opcional. La aplicación usa Angular, .NET, PostgreSQL y Keycloak.

Estamos en la **semana 4**. El primer recorrido de punta a punta se ensaya en la semana 5 y el candidato debe quedar validado al terminar la semana 6, listo para la semana 7. El plazo oficial del aula sigue por verificar. [Calendario, capacidad y criterios de entrega](docs/plan-sisplan/12-ENTREGA-SEMANA-7.md)

## Documentación

- [Preparación de F0 y casos iniciales de QA](docs/plan-sisplan/13-ARRANQUE-F0.md)
- [Plan vigente · fases, subfases, dependencias y pruebas](docs/plan-sisplan/11-PLAN-POR-FASES-SRS-V2.md)
- [Adaptación y alcance del SRS v2](docs/plan-sisplan/10-ADAPTACION-SRS-V2.md)

- [Arranque local y pruebas de la aplicación](docs/plan-sisplan/06-PRIMER-INCREMENTO.md)
- [QA histórico del recorrido PEI–POA v1](docs/plan-sisplan/09-QA-PEI-POA.md)

- [Plan base v1.0 con reglas, herramientas, responsables y entregables](docs/plan-sisplan/01-PLAN-DE-CONSTRUCCION.md)
- [Verificación histórica del SRS v1, los Excel y la consigna](docs/plan-sisplan/05-TRAZABILIDAD.md#verificación-del-srs-y-los-excel)
- [Estado del proyecto](docs/ESTADO.md)
- [Método de trabajo](docs/METODO-DE-TRABAJO.md)
- [Decisiones y asuntos pendientes](docs/DECISIONES.md)
- [Uso del portal y participación del equipo](docs/PORTAL.md)
- [Identidad visual de SISPLAN](docs/IDENTIDAD.md)
- [Actividades no planificadas](docs/AMPLIACION-NO-PLANIFICADA.md)
- [Referencia del reto anterior](docs/REFERENCIA-RETO-ANTERIOR.md)
