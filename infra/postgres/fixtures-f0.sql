-- Datos ficticios para una base local desechable, como propietario del esquema.
-- Las dos instituciones empiezan con ambos módulos. QA cambia las opciones
-- de forma explícita para ensayar las dos configuraciones autónomas.
BEGIN;
INSERT INTO sisplan.institucion(id,nombre) VALUES
    ('00000000-0000-4000-8000-000000000001','Instituto Horizonte'),
    ('00000000-0000-4000-8000-000000000002','Instituto Bahía')
ON CONFLICT DO NOTHING;
WITH dependencias(id,institucion,nombre,activa) AS (VALUES
    (101,1,'Planificación',true),(102,1,'Tecnología',true),(103,1,'Archivo anterior',false),
    (201,2,'Planificación',true),(202,2,'Operaciones',true))
INSERT INTO sisplan.dependencia(id,institucion_id,nombre,activa)
SELECT ('00000000-0000-4000-8000-' || lpad(id::text,12,'0'))::uuid,
       ('00000000-0000-4000-8000-' || lpad(institucion::text,12,'0'))::uuid,nombre,activa
FROM dependencias ON CONFLICT DO NOTHING;

WITH periodos(id,institucion,nombre,inicio,fin) AS (VALUES
    (1001,1,'PEI 2026–2029',date '2026-01-01',date '2029-12-31'),
    (1002,1,'POA 2026',date '2026-01-01',date '2026-12-31'),
    (2001,2,'PEI 2026–2030',date '2026-01-01',date '2030-12-31'),
    (2002,2,'POA 2026',date '2026-01-01',date '2026-12-31'))
INSERT INTO sisplan.periodo(id,institucion_id,nombre,fecha_inicio,fecha_fin)
SELECT ('00000000-0000-4000-8000-' || lpad(id::text,12,'0'))::uuid,
       ('00000000-0000-4000-8000-' || lpad(institucion::text,12,'0'))::uuid,nombre,inicio,fin
FROM periodos ON CONFLICT DO NOTHING;
INSERT INTO sisplan.unidad_medida(institucion_id,codigo,nombre)
SELECT i.id,u.codigo,u.nombre FROM sisplan.institucion i
CROSS JOIN (VALUES ('unidad','Unidad'),('porcentaje','Porcentaje')) AS u(codigo,nombre)
WHERE i.id IN ('00000000-0000-4000-8000-000000000001','00000000-0000-4000-8000-000000000002')
ON CONFLICT DO NOTHING;
INSERT INTO sisplan.unidad_medida(institucion_id,codigo,nombre) VALUES
    ('00000000-0000-4000-8000-000000000001','expediente','Expediente'),
    ('00000000-0000-4000-8000-000000000002','inspeccion','Inspección')
ON CONFLICT DO NOTHING;

WITH cuentas(cuenta,institucion,rol,dependencia) AS (VALUES
    (1,1,'SuperAdministrador',NULL),(2,1,'AdministradorInstitucional',NULL),
    (3,1,'DirectorPlanificacion',NULL),(4,1,'AnalistaPlanificacion',NULL),
    (5,1,'ResponsableArea',101),(6,1,'DirectorArea',101),(7,1,'Auditor',NULL),
    (8,1,'MaximaAutoridad',NULL),(9,1,'ComiteEstrategico',NULL),
    (10,2,'AnalistaPlanificacion',NULL),(11,2,'ResponsableArea',201),
    (12,1,'ResponsableArea',102),(13,1,'ResponsableArea',103),
    (14,1,'AnalistaPlanificacion',NULL),(14,2,'Auditor',NULL))
INSERT INTO sisplan.membresia(emisor,sujeto,institucion_id,rol,dependencia_id)
SELECT 'http://127.0.0.1:8080/realms/sisplan',
       '30000000-0000-4000-8000-' || lpad(cuenta::text,12,'0'),
       ('00000000-0000-4000-8000-' || lpad(institucion::text,12,'0'))::uuid,rol,
       ('00000000-0000-4000-8000-' || lpad(dependencia::text,12,'0'))::uuid
FROM cuentas ON CONFLICT DO NOTHING;
COMMIT;
