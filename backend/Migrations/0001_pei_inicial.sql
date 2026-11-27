CREATE TABLE sisplan.pei (
    id uuid PRIMARY KEY,
    creado_en timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE sisplan.pei_version (
    id uuid PRIMARY KEY,
    pei_id uuid NOT NULL REFERENCES sisplan.pei(id) ON DELETE RESTRICT,
    numero integer NOT NULL DEFAULT 1 CHECK (numero > 0),
    revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0),
    nombre text NOT NULL CHECK (char_length(btrim(nombre)) BETWEEN 3 AND 200),
    tipo text NOT NULL CHECK (tipo IN ('ordinario', 'extraordinario')),
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    mision text NOT NULL CHECK (char_length(btrim(mision)) BETWEEN 1 AND 4000),
    vision text NOT NULL CHECK (char_length(btrim(vision)) BETWEEN 1 AND 4000),
    valores text NOT NULL CHECK (char_length(btrim(valores)) BETWEEN 1 AND 4000),
    estado text NOT NULL DEFAULT 'borrador' CHECK (estado = 'borrador'),
    creado_en timestamptz NOT NULL DEFAULT clock_timestamp(),
    actualizado_en timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT pei_version_numero_unico UNIQUE (pei_id, numero),
    CONSTRAINT pei_version_fechas_ordenadas CHECK (fecha_fin >= fecha_inicio),
    CONSTRAINT pei_version_anios_segun_tipo CHECK (
        EXTRACT(YEAR FROM fecha_fin) - EXTRACT(YEAR FROM fecha_inicio) + 1
        = CASE tipo WHEN 'ordinario' THEN 4 WHEN 'extraordinario' THEN 5 END
    )
);

CREATE INDEX pei_creado_en_idx ON sisplan.pei (creado_en DESC, id DESC);
