ALTER TABLE sisplan.pei_version DROP CONSTRAINT pei_version_estado_check;
ALTER TABLE sisplan.pei_version
    ADD CONSTRAINT pei_version_estado_check
    CHECK (estado IN ('borrador', 'en_revision', 'aprobado', 'publicado')),
    ADD CONSTRAINT pei_version_identidad_padre UNIQUE (id, pei_id);

CREATE TABLE sisplan.pei_transicion (
    pei_version_id uuid NOT NULL REFERENCES sisplan.pei_version(id) ON DELETE RESTRICT,
    revision bigint NOT NULL CHECK (revision > 1),
    accion text NOT NULL CHECK (accion IN ('enviar', 'aprobar', 'devolver', 'publicar')),
    estado_anterior text NOT NULL,
    estado_nuevo text NOT NULL,
    emisor text NOT NULL,
    sujeto text NOT NULL,
    motivo text,
    registrada_en timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (pei_version_id, revision)
);

CREATE TABLE sisplan.poa (
    id uuid PRIMARY KEY,
    pei_id uuid NOT NULL,
    pei_version_id uuid NOT NULL,
    anio integer NOT NULL CHECK (anio BETWEEN 1 AND 9999),
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    estado text NOT NULL DEFAULT 'formulacion'
        CHECK (estado IN ('formulacion', 'en_revision', 'aprobado', 'en_ejecucion', 'cerrado')),
    UNIQUE (pei_id, anio),
    FOREIGN KEY (pei_version_id, pei_id) REFERENCES sisplan.pei_version(id, pei_id) ON DELETE RESTRICT,
    CHECK (fecha_inicio <= fecha_fin AND EXTRACT(YEAR FROM fecha_inicio) = anio AND EXTRACT(YEAR FROM fecha_fin) = anio)
);

CREATE TABLE sisplan.poa_evaluacion (
    id uuid PRIMARY KEY,
    poa_id uuid NOT NULL REFERENCES sisplan.poa(id) ON DELETE RESTRICT,
    cuatrimestre integer NOT NULL CHECK (cuatrimestre BETWEEN 1 AND 3),
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    estado text NOT NULL CHECK (estado IN ('generada', 'fuera_de_vigencia', 'en_revision', 'emitida', 'publicada')),
    UNIQUE (poa_id, cuatrimestre),
    CHECK (fecha_inicio <= fecha_fin)
);
