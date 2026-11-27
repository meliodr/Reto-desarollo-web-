ALTER TABLE sisplan.objetivo
    ADD COLUMN activo boolean NOT NULL DEFAULT true,
    ADD COLUMN revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0);

CREATE TABLE sisplan.riesgo_estrategico (
    id uuid PRIMARY KEY,
    institucion_id uuid NOT NULL DEFAULT sisplan.institucion_actual(),
    objetivo_id uuid NOT NULL,
    nombre text NOT NULL CHECK (char_length(btrim(nombre)) BETWEEN 3 AND 200),
    descripcion text NOT NULL CHECK (char_length(btrim(descripcion)) BETWEEN 1 AND 4000),
    probabilidad integer NOT NULL CHECK (probabilidad BETWEEN 1 AND 5),
    impacto integer NOT NULL CHECK (impacto BETWEEN 1 AND 5),
    tratamiento text CHECK (char_length(btrim(tratamiento)) BETWEEN 1 AND 200),
    accion_mitigacion text CHECK (char_length(btrim(accion_mitigacion)) BETWEEN 1 AND 4000),
    responsable text CHECK (char_length(btrim(responsable)) BETWEEN 1 AND 200),
    fecha_revision date,
    evidencia_url text CHECK (char_length(evidencia_url) <= 2048 AND evidencia_url ~* '^https://[^[:space:]]+$'),
    cerrado boolean NOT NULL DEFAULT false,
    revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0),
    FOREIGN KEY (institucion_id, objetivo_id) REFERENCES sisplan.objetivo(institucion_id,id),
    CONSTRAINT riesgo_alto_cerrado_con_mitigacion CHECK (
        NOT cerrado OR probabilidad * impacto < 15 OR
        (tratamiento IS NOT NULL AND accion_mitigacion IS NOT NULL AND responsable IS NOT NULL
         AND fecha_revision IS NOT NULL AND evidencia_url IS NOT NULL)
    )
);
CREATE INDEX riesgo_estrategico_por_objetivo ON sisplan.riesgo_estrategico(institucion_id,objetivo_id,id);

ALTER TABLE sisplan.riesgo_estrategico ENABLE ROW LEVEL SECURITY;
ALTER TABLE sisplan.riesgo_estrategico FORCE ROW LEVEL SECURITY;
CREATE POLICY lectura ON sisplan.riesgo_estrategico FOR SELECT TO sisplan_app USING (
    institucion_id=(SELECT sisplan.institucion_actual()) AND (SELECT sisplan.rol_modulo('pei')) IS NOT NULL
);
CREATE POLICY alta ON sisplan.riesgo_estrategico FOR INSERT TO sisplan_app WITH CHECK (
    institucion_id=(SELECT sisplan.institucion_actual())
    AND (SELECT sisplan.rol_modulo('pei')) IN ('AnalistaPlanificacion','Planificador')
);
CREATE POLICY cambio ON sisplan.riesgo_estrategico FOR UPDATE TO sisplan_app USING (
    institucion_id=(SELECT sisplan.institucion_actual())
    AND (SELECT sisplan.rol_modulo('pei')) IN ('AnalistaPlanificacion','Planificador')
) WITH CHECK (
    institucion_id=(SELECT sisplan.institucion_actual())
    AND (SELECT sisplan.rol_modulo('pei')) IN ('AnalistaPlanificacion','Planificador')
);
GRANT SELECT,INSERT ON sisplan.riesgo_estrategico TO sisplan_app;
GRANT UPDATE (nombre,descripcion,probabilidad,impacto,tratamiento,accion_mitigacion,
              responsable,fecha_revision,evidencia_url,cerrado,revision) ON sisplan.riesgo_estrategico TO sisplan_app;
CREATE TRIGGER auditar_cambios AFTER INSERT OR UPDATE ON sisplan.riesgo_estrategico
FOR EACH ROW EXECUTE FUNCTION sisplan.registrar_cambio();
