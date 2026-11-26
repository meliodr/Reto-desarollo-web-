-- Ejecutar después de fixtures-f0.sql en una base desechable, con una cuenta que
-- pueda SET ROLE sisplan_app. Los datos del caso se revierten juntos al final.
BEGIN;
SET LOCAL ROLE sisplan_app;
DO $$
BEGIN
    IF EXISTS (SELECT FROM pg_roles WHERE rolname = current_user AND (rolsuper OR rolbypassrls))
       OR EXISTS (SELECT FROM pg_class WHERE oid = 'sisplan.pei'::regclass AND pg_get_userbyid(relowner) = current_user)
    THEN RAISE EXCEPTION 'F0-05: la cuenta de prueba elude RLS.';
    END IF;
    IF EXISTS (SELECT FROM sisplan.pei) OR EXISTS (SELECT FROM sisplan.poa)
       OR EXISTS (SELECT FROM sisplan.auditoria)
    THEN RAISE EXCEPTION 'F0-05: una conexión sin contexto devolvió datos institucionales.';
    END IF;
END $$;

SELECT set_config('sisplan.emisor', 'http://127.0.0.1:8080/realms/sisplan', true),
       set_config('sisplan.sujeto', '30000000-0000-4000-8000-000000000004', true),
       set_config('sisplan.institucion_id', '00000000-0000-4000-8000-000000000001', true);
INSERT INTO sisplan.pei(id) VALUES ('50000000-0000-4000-8000-000000000001');
INSERT INTO sisplan.pei_version(id, pei_id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores)
VALUES ('50000000-0000-4000-8000-000000000002', '50000000-0000-4000-8000-000000000001',
        'PEI SQL de A', 'ordinario', '2040-01-01', '2043-12-31', 'Atender', 'Mejorar', 'Servicio');
INSERT INTO sisplan.poa(id, anio, fecha_inicio, fecha_fin)
VALUES ('50000000-0000-4000-8000-000000000003', 2040, '2040-01-01', '2040-12-31');

SELECT set_config('sisplan.sujeto', '30000000-0000-4000-8000-000000000010', true),
       set_config('sisplan.institucion_id', '00000000-0000-4000-8000-000000000002', true);
INSERT INTO sisplan.poa(id, anio, fecha_inicio, fecha_fin)
VALUES ('50000000-0000-4000-8000-000000000004', 2040, '2040-01-01', '2040-12-31');
DO $$
BEGIN
    IF EXISTS (SELECT FROM sisplan.pei_version WHERE id = '50000000-0000-4000-8000-000000000002')
       OR EXISTS (SELECT FROM sisplan.poa WHERE id = '50000000-0000-4000-8000-000000000003')
       OR NOT EXISTS (SELECT FROM sisplan.poa WHERE id = '50000000-0000-4000-8000-000000000004')
    THEN RAISE EXCEPTION 'F0-05: la conexión reutilizada conservó filas del contexto anterior.';
    END IF;
    UPDATE sisplan.pei_version SET mision = 'Cambio de otra institución'
    WHERE id = '50000000-0000-4000-8000-000000000002';
    IF FOUND THEN RAISE EXCEPTION 'F0-04: actualización de un PEI de otra institución.'; END IF;
    BEGIN
        INSERT INTO sisplan.eje(id, pei_version_id)
        VALUES ('50000000-0000-4000-8000-000000000005','50000000-0000-4000-8000-000000000002');
        RAISE EXCEPTION 'F0-04: se creó un vínculo entre dos instituciones.';
    EXCEPTION WHEN foreign_key_violation OR insufficient_privilege THEN NULL;
    END;
    BEGIN
        INSERT INTO sisplan.poa(id, anio, fecha_inicio, fecha_fin)
        VALUES ('50000000-0000-4000-8000-000000000006', 2040, '2040-01-01', '2040-12-31');
        RAISE EXCEPTION 'F0-03: se aceptó otro POA autónomo del mismo año.';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
END $$;

SELECT set_config('sisplan.sujeto', '30000000-0000-4000-8000-000000000007', true),
       set_config('sisplan.institucion_id', '00000000-0000-4000-8000-000000000001', true);
DO $$
BEGIN
    IF EXISTS (SELECT FROM sisplan.auditoria WHERE institucion_id <> '00000000-0000-4000-8000-000000000001')
       OR NOT EXISTS (SELECT FROM sisplan.auditoria WHERE entidad = 'poa' AND accion = 'INSERT'
                     AND referencia::text LIKE '%50000000-0000-4000-8000-000000000003%'
                     AND emisor = 'http://127.0.0.1:8080/realms/sisplan'
                     AND sujeto = '30000000-0000-4000-8000-000000000004' AND fecha IS NOT NULL)
    THEN RAISE EXCEPTION 'F0-06: auditoría ausente, incompleta o de otra institución.'; END IF;
    IF NOT EXISTS (SELECT FROM sisplan.pei_version WHERE id = '50000000-0000-4000-8000-000000000002' AND mision = 'Atender')
    THEN RAISE EXCEPTION 'F0-04: la versión original fue alterada.'; END IF;
END $$;
ROLLBACK;
