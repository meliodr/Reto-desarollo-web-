ALTER TABLE sisplan.pei_version
    ADD COLUMN catalogo_digemaps_cargado boolean NOT NULL DEFAULT false;

-- El catálogo anterior identificaba cada fila con el código DIG-04 a DIG-23.
-- 0009 conserva el origen de los indicadores de ese catálogo.
ALTER TABLE sisplan.pei_version NO FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.eje NO FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.objetivo NO FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.indicador NO FORCE ROW LEVEL SECURITY;
UPDATE sisplan.pei_version v
SET catalogo_digemaps_cargado = true
WHERE 20 = (
    SELECT count(DISTINCT o.codigo)
    FROM sisplan.eje e
    JOIN sisplan.objetivo o ON o.eje_id = e.id
    JOIN sisplan.indicador i ON i.objetivo_id = o.id
    WHERE e.pei_version_id = v.id AND i.origen_digemaps
      AND o.codigo ~ '^DIG-(0[4-9]|1[0-9]|2[0-3])$'
);

ALTER TABLE sisplan.pei_version FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.eje FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.objetivo FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.indicador FORCE ROW LEVEL SECURITY;
