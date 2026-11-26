-- Ejecutar tras el rechazo de 0008 por falta de correspondencia para el POA autónomo.
DO $$
BEGIN
    IF to_regclass('sisplan.institucion') IS NOT NULL
       OR EXISTS (SELECT FROM information_schema.columns WHERE table_schema='sisplan' AND column_name='institucion_id'
                  AND table_name IN ('pei','pei_version','poa'))
       OR EXISTS (SELECT FROM sisplan.migracion WHERE nombre='0008_base_institucional.sql')
       OR (SELECT count(*) FROM sisplan.migracion) <> 7
       OR (SELECT count(*) FROM sisplan.f0_mapa_historico) <> 1
       OR NOT EXISTS (SELECT FROM sisplan.poa WHERE id='40000000-0000-4000-8000-000000000008'
                      AND presupuesto_referencia=1000.25 AND pei_id IS NULL)
    THEN
        RAISE EXCEPTION 'F0-06: la asignación incompleta alteró el esquema, el historial o los datos.';
    END IF;
END $$;
INSERT INTO sisplan.f0_mapa_historico(entidad, registro_id, institucion_id)
VALUES ('poa','40000000-0000-4000-8000-000000000008','00000000-0000-4000-8000-000000000002');
