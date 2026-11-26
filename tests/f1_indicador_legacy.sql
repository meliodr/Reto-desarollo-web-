-- Reproducir un indicador válido antes de F1 en la base desechable.
CREATE FUNCTION sisplan.f1_indicador_legacy_ci() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $$
BEGIN
    NEW.sentido := NULL;
    NEW.metodo_calculo := NULL;
    RETURN NEW;
END $$;

CREATE TRIGGER f1_indicador_legacy_ci BEFORE INSERT ON sisplan.indicador
FOR EACH ROW WHEN (NEW.nombre = 'Indicador histórico F0 de prueba')
EXECUTE FUNCTION sisplan.f1_indicador_legacy_ci();
