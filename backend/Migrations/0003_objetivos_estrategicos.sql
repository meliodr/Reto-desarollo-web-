CREATE TABLE sisplan.objetivo (
    id uuid PRIMARY KEY,
    eje_id uuid NOT NULL,
    eje_revision bigint NOT NULL,
    codigo text NOT NULL CHECK (char_length(btrim(codigo)) BETWEEN 1 AND 50),
    nombre text NOT NULL CHECK (char_length(btrim(nombre)) BETWEEN 3 AND 200),
    descripcion text NOT NULL CHECK (char_length(btrim(descripcion)) BETWEEN 1 AND 4000),
    resultado_esperado text NOT NULL CHECK (char_length(btrim(resultado_esperado)) BETWEEN 1 AND 4000),
    linea_accion text NOT NULL CHECK (char_length(btrim(linea_accion)) BETWEEN 1 AND 4000),
    CONSTRAINT objetivo_eje_revision_fk FOREIGN KEY (eje_id, eje_revision)
        REFERENCES sisplan.eje_version(eje_id, revision) ON DELETE RESTRICT,
    CONSTRAINT objetivo_codigo_por_eje UNIQUE (eje_id, codigo)
);
