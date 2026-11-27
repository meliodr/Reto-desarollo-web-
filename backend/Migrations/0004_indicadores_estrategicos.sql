CREATE TABLE sisplan.indicador (
    id uuid PRIMARY KEY,
    objetivo_id uuid NOT NULL REFERENCES sisplan.objetivo(id) ON DELETE RESTRICT,
    nombre text NOT NULL CHECK (char_length(btrim(nombre)) BETWEEN 3 AND 200),
    formula text NOT NULL CHECK (char_length(btrim(formula)) BETWEEN 1 AND 4000),
    unidad text NOT NULL CHECK (char_length(btrim(unidad)) BETWEEN 1 AND 100),
    linea_base numeric(16,4) NOT NULL,
    meta numeric(16,4) NOT NULL,
    frecuencia text NOT NULL CHECK (char_length(btrim(frecuencia)) BETWEEN 1 AND 100)
);

CREATE INDEX indicador_por_objetivo ON sisplan.indicador (objetivo_id, nombre, id);

CREATE TABLE sisplan.indicador_medicion (
    id uuid PRIMARY KEY,
    indicador_id uuid NOT NULL REFERENCES sisplan.indicador(id) ON DELETE RESTRICT,
    periodo date NOT NULL,
    valor numeric(16,4) NOT NULL,
    CONSTRAINT indicador_medicion_periodo_unico UNIQUE (indicador_id, periodo)
);
