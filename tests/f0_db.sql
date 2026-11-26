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
-- Un único backend físico alterna las identidades sin cerrar la conexión.
DO $$
DECLARE
    cuenta integer;
BEGIN
    FOR cuenta IN 1..9 LOOP
        PERFORM set_config('sisplan.sujeto','30000000-0000-4000-8000-' || lpad(cuenta::text,12,'0'),true);
        IF NOT EXISTS (SELECT FROM sisplan.pei WHERE id='50000000-0000-4000-8000-000000000001') THEN
            RAISE EXCEPTION 'F0-02: el perfil % perdió su lectura del PEI.', cuenta;
        END IF;
        IF EXISTS (SELECT FROM sisplan.auditoria WHERE referencia='50000000-0000-4000-8000-000000000003')
           IS DISTINCT FROM (cuenta IN (2,7)) THEN
            RAISE EXCEPTION 'F0-02: el perfil % obtuvo un alcance de auditoría incorrecto.', cuenta;
        END IF;
    END LOOP;
    PERFORM set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000005',true);
    INSERT INTO sisplan.poa(id,anio,fecha_inicio,fecha_fin)
    VALUES ('50000000-0000-4000-8000-000000000008',2041,'2041-01-01','2041-12-31');
    BEGIN
        INSERT INTO sisplan.poa(id,anio,fecha_inicio,fecha_fin,dependencia_id)
        VALUES ('50000000-0000-4000-8000-000000000009',2042,'2042-01-01','2042-12-31',
                '00000000-0000-4000-8000-000000000102');
        RAISE EXCEPTION 'F0-02: se escribió desde otra dependencia.';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    PERFORM set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000012',true);
    IF EXISTS (SELECT FROM sisplan.poa WHERE id='50000000-0000-4000-8000-000000000008') THEN
        RAISE EXCEPTION 'F0-02: el área de Tecnología puede leer el POA de Planificación.';
    END IF;
    PERFORM set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000013',true);
    IF EXISTS (SELECT FROM sisplan.pei) OR EXISTS (SELECT FROM sisplan.poa) THEN
        RAISE EXCEPTION 'F0-02: el área inactiva obtuvo datos.';
    END IF;
END $$;
SELECT set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000004',true);
UPDATE sisplan.pei_version SET estado='publicado' WHERE id='50000000-0000-4000-8000-000000000002';
INSERT INTO sisplan.pei_transicion(pei_version_id,revision,accion,estado_anterior,estado_nuevo,emisor,sujeto)
VALUES ('50000000-0000-4000-8000-000000000002',2,'enviar','borrador','en_revision',
        'http://127.0.0.1:8080/realms/sisplan','30000000-0000-4000-8000-000000000004');
DO $$
BEGIN
    BEGIN
        UPDATE sisplan.pei_transicion SET motivo='Nota posterior'
        WHERE pei_version_id='50000000-0000-4000-8000-000000000002' AND revision=2;
        IF FOUND THEN RAISE EXCEPTION 'F0-06: la API editó una transición histórica.'; END IF;
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    BEGIN
        DELETE FROM sisplan.pei_transicion
        WHERE pei_version_id='50000000-0000-4000-8000-000000000002' AND revision=2;
        IF FOUND THEN RAISE EXCEPTION 'F0-06: la API eliminó una transición histórica.'; END IF;
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    IF NOT EXISTS (SELECT FROM sisplan.pei_transicion WHERE pei_version_id='50000000-0000-4000-8000-000000000002'
                   AND revision=2 AND motivo IS NULL AND accion='enviar') THEN
        RAISE EXCEPTION 'F0-06: no se conservó la transición original.';
    END IF;
    BEGIN
        INSERT INTO sisplan.poa(id,anio,fecha_inicio,fecha_fin,dependencia_id)
        VALUES ('50000000-0000-4000-8000-000000000010',2044,'2044-01-01','2044-12-31',
                '00000000-0000-4000-8000-000000000103');
        RAISE EXCEPTION 'F0-02: el analista asignó un POA a una dependencia inactiva.';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
END $$;
INSERT INTO sisplan.poa(id,anio,fecha_inicio,fecha_fin,pei_id,pei_version_id)
VALUES ('50000000-0000-4000-8000-000000000011',2043,'2043-01-01','2043-12-31',
        '50000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000002');

RESET ROLE;
INSERT INTO sisplan.membresia(emisor,sujeto,institucion_id,rol,dependencia_id)
VALUES ('http://127.0.0.1:8080/realms/sisplan','30000000-0000-4000-8000-000000000005',
        '00000000-0000-4000-8000-000000000002','ResponsableArea','00000000-0000-4000-8000-000000000201');
SET LOCAL ROLE sisplan_app;
SELECT set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000005',true);
DO $$
DECLARE
    institucion uuid;
    visibles uuid[];
BEGIN
    FOREACH institucion IN ARRAY ARRAY['00000000-0000-4000-8000-000000000001'::uuid,
                                       '00000000-0000-4000-8000-000000000002'::uuid] LOOP
        PERFORM set_config('sisplan.institucion_id',institucion::text,true);
        SELECT array_agg(id) INTO visibles FROM sisplan.dependencia;
        IF visibles IS DISTINCT FROM ARRAY[CASE institucion WHEN '00000000-0000-4000-8000-000000000001'
               THEN '00000000-0000-4000-8000-000000000101'::uuid ELSE '00000000-0000-4000-8000-000000000201'::uuid END] THEN
            RAISE EXCEPTION 'F0-04: la cuenta de dos áreas recibió dependencias ajenas al contexto %.', institucion;
        END IF;
    END LOOP;
END $$;
SELECT set_config('sisplan.institucion_id','00000000-0000-4000-8000-000000000001',true);
RESET ROLE;
UPDATE sisplan.institucion SET pei_habilitado=false WHERE id='00000000-0000-4000-8000-000000000001';
SET LOCAL ROLE sisplan_app;
SELECT set_config('sisplan.sujeto','30000000-0000-4000-8000-000000000004',true);
DO $$
BEGIN
    IF EXISTS (SELECT FROM sisplan.pei) OR NOT EXISTS (SELECT FROM sisplan.poa WHERE id='50000000-0000-4000-8000-000000000003') THEN
        RAISE EXCEPTION 'F0-07: el modo solo POA no aisló los módulos.';
    END IF;
    UPDATE sisplan.poa SET presupuesto_referencia=15.25 WHERE id='50000000-0000-4000-8000-000000000011';
    IF NOT FOUND OR NOT EXISTS (SELECT FROM sisplan.poa WHERE id='50000000-0000-4000-8000-000000000011'
                                AND presupuesto_referencia=15.25 AND pei_id='50000000-0000-4000-8000-000000000001') THEN
        RAISE EXCEPTION 'F0-07: el POA anterior perdió su referencia o su operación autónoma.';
    END IF;
    BEGIN
        INSERT INTO sisplan.poa(id,anio,fecha_inicio,fecha_fin,pei_id,pei_version_id)
        VALUES ('50000000-0000-4000-8000-000000000012',2042,'2042-01-01','2042-12-31',
                '50000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000002');
        RAISE EXCEPTION 'F0-07: se creó un vínculo nuevo con el módulo PEI deshabilitado.';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
END $$;
RESET ROLE;
UPDATE sisplan.institucion SET pei_habilitado=true,poa_habilitado=false
WHERE id='00000000-0000-4000-8000-000000000001';
SET LOCAL ROLE sisplan_app;
DO $$
BEGIN
    IF EXISTS (SELECT FROM sisplan.poa) OR NOT EXISTS (SELECT FROM sisplan.pei WHERE id='50000000-0000-4000-8000-000000000001') THEN
        RAISE EXCEPTION 'F0-07: el modo solo PEI no aisló los módulos.';
    END IF;
END $$;
ROLLBACK;
