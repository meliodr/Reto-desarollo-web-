ALTER TABLE sisplan.indicador
    ADD COLUMN sentido text CHECK (sentido IN ('ascendente', 'descendente')),
    ADD COLUMN metodo_calculo text CHECK (metodo_calculo IN ('valor_directo', 'variacion_absoluta', 'variacion_porcentual')),
    ADD COLUMN revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0),
    ADD COLUMN origen_digemaps boolean NOT NULL DEFAULT false,
    ADD COLUMN listo_para_medir boolean GENERATED ALWAYS AS (
        formula IS NOT NULL AND unidad IS NOT NULL AND frecuencia IS NOT NULL AND meta IS NOT NULL
        AND sentido IS NOT NULL AND metodo_calculo IS NOT NULL AND (
            metodo_calculo = 'valor_directo'
            OR (metodo_calculo = 'variacion_absoluta' AND linea_base IS NOT NULL)
            OR (metodo_calculo = 'variacion_porcentual' AND linea_base IS NOT NULL AND linea_base <> 0)
        )
    ) STORED;

-- Antes de F1 DIGEMAPS era la única carga que creaba indicadores sin configuración.
-- Reconocer las 20 parejas código/nombre exactas del catálogo original en sus seis ejes.
-- Permitir la retrocarga al propietario del esquema durante esta transacción.
ALTER TABLE sisplan.eje NO FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.objetivo NO FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.indicador NO FORCE ROW LEVEL SECURITY;
WITH candidatos AS (
    SELECT i.id, e.id AS eje_id, e.pei_version_id, o.codigo, i.nombre
    FROM sisplan.indicador i
    JOIN sisplan.objetivo o ON o.id = i.objetivo_id
    JOIN sisplan.eje e ON e.id = o.eje_id
    WHERE o.codigo ~ '^DIG-(0[4-9]|1[0-9]|2[0-3])$'
      AND i.formula IS NULL AND i.unidad IS NULL AND i.linea_base IS NULL
      AND i.meta IS NULL AND i.frecuencia IS NULL
),
catalogos AS (
    SELECT pei_version_id FROM candidatos
    GROUP BY pei_version_id
    HAVING count(*) = 20 AND count(DISTINCT codigo) = 20 AND count(DISTINCT eje_id) = 6
       AND sha256(convert_to(string_agg(codigo || ':' || nombre, E'\n' ORDER BY codigo COLLATE "C"), 'UTF8'))
           = decode('d966a785be18c92c25afeb9e8292491af370e0173bec70763ab571280f3f040a', 'hex')
)
UPDATE sisplan.indicador i SET origen_digemaps = true
FROM candidatos c JOIN catalogos v ON v.pei_version_id = c.pei_version_id
WHERE c.id = i.id;
ALTER TABLE sisplan.eje FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.objetivo FORCE ROW LEVEL SECURITY;
ALTER TABLE sisplan.indicador FORCE ROW LEVEL SECURITY;

-- Las mediciones existentes conservan su valor aunque el indicador requiera completar los nuevos parámetros.
CREATE FUNCTION sisplan.exigir_indicador_configurado() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog AS $$
BEGIN
    PERFORM FROM sisplan.indicador i
    WHERE i.id=NEW.indicador_id AND i.institucion_id=NEW.institucion_id AND i.listo_para_medir
    FOR SHARE;
    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'El indicador requiere configuración antes de medir.';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER exigir_indicador_configurado
BEFORE INSERT OR UPDATE OF indicador_id, periodo, valor ON sisplan.indicador_medicion
FOR EACH ROW EXECUTE FUNCTION sisplan.exigir_indicador_configurado();
