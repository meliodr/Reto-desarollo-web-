-- Ejecutar con la cuenta de migraciones, después de repetir --migrate.
DO $$
DECLARE
    institucion_a constant uuid := '00000000-0000-4000-8000-000000000001';
    institucion_b constant uuid := '00000000-0000-4000-8000-000000000002';
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sisplan.pei p
        JOIN sisplan.pei_version v ON v.pei_id = p.id
        JOIN sisplan.eje e ON e.pei_version_id = v.id
        JOIN sisplan.eje_version ev ON ev.eje_id = e.id
        JOIN sisplan.objetivo o ON (o.eje_id, o.eje_revision) = (e.id, ev.revision)
        JOIN sisplan.indicador i ON i.objetivo_id = o.id
        JOIN sisplan.indicador_medicion m ON m.indicador_id = i.id
        JOIN sisplan.poa a ON (a.pei_id, a.pei_version_id) = (p.id, v.id)
        JOIN sisplan.poa_evaluacion eval ON eval.poa_id = a.id
        WHERE p.id = '40000000-0000-4000-8000-000000000001'
          AND a.id = '40000000-0000-4000-8000-000000000007'
          AND eval.id = '40000000-0000-4000-8000-000000000009'
          AND m.id = '40000000-0000-4000-8000-000000000006' AND m.valor = 17.25
          AND v.nombre = 'PEI anterior a F0' AND a.presupuesto_referencia IS NULL
          AND institucion_a = ALL (ARRAY[p.institucion_id, v.institucion_id, e.institucion_id, ev.institucion_id,
                             o.institucion_id, i.institucion_id, m.institucion_id, a.institucion_id, eval.institucion_id])
    ) THEN
        RAISE EXCEPTION 'F0-06: cambió la identidad, el destino, el presupuesto o un vínculo de los datos históricos.';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM sisplan.poa WHERE id = '40000000-0000-4000-8000-000000000008'
          AND pei_id IS NULL AND pei_version_id IS NULL AND anio = 2030
          AND presupuesto_referencia = 1000.25 AND institucion_id = institucion_b
    ) THEN
        RAISE EXCEPTION 'F0-06: el POA autónomo perdió su asignación o su importe.';
    END IF;
END $$;
