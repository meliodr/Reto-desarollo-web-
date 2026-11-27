-- Sólo en la base desechable de CI, provocar un fallo después de que avance la carga.
CREATE FUNCTION sisplan.f1_fallo_catalogo_ci() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM sisplan.eje e JOIN sisplan.pei_version v ON v.id = e.pei_version_id
        WHERE e.id = NEW.eje_id AND v.nombre = 'PEI de prueba con falla transaccional DIGEMAPS'
    ) THEN
        RAISE check_violation USING MESSAGE = 'Fallo de carga de prueba de F1';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER f1_fallo_catalogo_ci BEFORE INSERT ON sisplan.objetivo
FOR EACH ROW WHEN (NEW.codigo = 'DIG-11') EXECUTE FUNCTION sisplan.f1_fallo_catalogo_ci();
