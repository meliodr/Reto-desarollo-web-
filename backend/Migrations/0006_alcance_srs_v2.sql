ALTER TABLE sisplan.poa
    ALTER COLUMN pei_id DROP NOT NULL,
    ALTER COLUMN pei_version_id DROP NOT NULL,
    ADD CONSTRAINT poa_vinculo_completo CHECK ((pei_id IS NULL) = (pei_version_id IS NULL));

ALTER TABLE sisplan.poa
    DROP CONSTRAINT poa_estado_check,
    ALTER COLUMN estado SET DEFAULT 'borrador',
    ADD CONSTRAINT poa_estado_check
        CHECK (estado IN ('borrador', 'formulacion', 'en_revision', 'observado', 'aprobado', 'rechazado', 'en_ejecucion', 'cerrado'));

CREATE UNIQUE INDEX poa_independiente_anual ON sisplan.poa (anio) WHERE pei_id IS NULL;

ALTER TABLE sisplan.pei_version DROP CONSTRAINT pei_version_estado_check;
ALTER TABLE sisplan.pei_version ADD CONSTRAINT pei_version_estado_check
    CHECK (estado IN ('borrador', 'en_revision', 'observado', 'corregido', 'aprobado', 'publicado'));

ALTER TABLE sisplan.pei_transicion DROP CONSTRAINT pei_transicion_accion_check;
ALTER TABLE sisplan.pei_transicion ADD CONSTRAINT pei_transicion_accion_check
    CHECK (accion IN ('enviar', 'revisar', 'validar_comite', 'aprobar', 'devolver', 'corregir', 'publicar'));

ALTER TABLE sisplan.indicador
    ALTER COLUMN formula DROP NOT NULL,
    ALTER COLUMN unidad DROP NOT NULL,
    ALTER COLUMN linea_base DROP NOT NULL,
    ALTER COLUMN meta DROP NOT NULL,
    ALTER COLUMN frecuencia DROP NOT NULL;
