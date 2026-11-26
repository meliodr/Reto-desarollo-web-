-- Reproducir un indicador válido antes de F1 en la base desechable.
CREATE FUNCTION sisplan.f1_indicador_legacy_ci() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $$
BEGIN
    -- Simular el dato anterior sólo en la versión inicial de este caso.
    IF EXISTS (
        SELECT 1 FROM sisplan.objetivo o
        JOIN sisplan.eje e ON e.id = o.eje_id
        JOIN sisplan.pei_version v ON v.id = e.pei_version_id
        WHERE o.id = NEW.objetivo_id
          AND v.numero = 1
          AND v.nombre = 'Plan estratégico 2170-01-01 a 2173-12-31'
    ) THEN
        NEW.sentido := NULL;
        NEW.metodo_calculo := NULL;
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER f1_indicador_legacy_ci BEFORE INSERT ON sisplan.indicador
FOR EACH ROW WHEN (NEW.nombre = 'Indicador histórico F0 de prueba')
EXECUTE FUNCTION sisplan.f1_indicador_legacy_ci();
