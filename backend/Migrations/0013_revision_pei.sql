ALTER TABLE sisplan.pei_version
    ADD COLUMN version_origen_id uuid,
    ADD COLUMN motivo_revision text,
    ADD COLUMN creada_por_emisor text,
    ADD COLUMN creada_por_sujeto text,
    ADD CONSTRAINT pei_version_origen_del_mismo_plan
        FOREIGN KEY (version_origen_id, pei_id) REFERENCES sisplan.pei_version(id, pei_id) ON DELETE RESTRICT,
    ADD CONSTRAINT pei_version_origen_con_motivo_y_actor CHECK (
        CASE WHEN version_origen_id IS NULL
             THEN motivo_revision IS NULL AND creada_por_emisor IS NULL AND creada_por_sujeto IS NULL
             ELSE numero > 1 AND version_origen_id <> id
                  AND motivo_revision IS NOT NULL AND char_length(btrim(motivo_revision)) BETWEEN 1 AND 4000
                  AND coalesce(creada_por_emisor, '') <> '' AND coalesce(creada_por_sujeto, '') <> ''
        END
    );

CREATE UNIQUE INDEX pei_version_un_sucesor_por_origen ON sisplan.pei_version (version_origen_id)
WHERE version_origen_id IS NOT NULL;
