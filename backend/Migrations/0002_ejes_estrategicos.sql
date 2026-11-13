CREATE TABLE sisplan.eje (
    id uuid PRIMARY KEY,
    pei_version_id uuid NOT NULL REFERENCES sisplan.pei_version(id) ON DELETE RESTRICT,
    creado_en timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX eje_por_version_pei ON sisplan.eje (pei_version_id, creado_en, id);

CREATE TABLE sisplan.eje_version (
    eje_id uuid NOT NULL REFERENCES sisplan.eje(id) ON DELETE RESTRICT,
    revision bigint NOT NULL CHECK (revision > 0),
    nombre text NOT NULL CHECK (char_length(btrim(nombre)) BETWEEN 3 AND 200),
    descripcion text CHECK (char_length(descripcion) <= 4000),
    activo boolean NOT NULL,
    actualizado_en timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (eje_id, revision)
);
