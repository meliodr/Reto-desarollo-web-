-- La cuenta de la API es independiente del propietario de datos y migraciones.
DO $$
BEGIN
    IF current_user = 'sisplan_app' OR NOT EXISTS (
        SELECT FROM pg_roles WHERE rolname = 'sisplan_app' AND rolcanlogin
          AND NOT (rolsuper OR rolbypassrls OR rolcreaterole OR rolcreatedb OR rolreplication)
    ) OR EXISTS (
        SELECT FROM pg_auth_members a JOIN pg_roles r ON r.oid=a.member WHERE r.rolname='sisplan_app'
    ) THEN
        RAISE EXCEPTION 'Provisione sisplan_app como login sin roles heredados ni privilegios administrativos; ejecute las migraciones con otra cuenta.';
    END IF;
END $$;

-- Ejecutar como migrador antes de 0008 si la instalación ya contiene planes.
-- Registrar el destino aprobado y una fila por PEI y por POA autónomo.
CREATE TABLE IF NOT EXISTS sisplan.f0_destino_historico (
    id uuid PRIMARY KEY,
    nombre text NOT NULL CHECK (btrim(nombre) <> '')
);
CREATE TABLE IF NOT EXISTS sisplan.f0_mapa_historico (
    entidad text NOT NULL CHECK (entidad IN ('pei','poa')),
    registro_id uuid NOT NULL,
    institucion_id uuid NOT NULL REFERENCES sisplan.f0_destino_historico(id),
    PRIMARY KEY (entidad, registro_id)
);

DO $$
BEGIN
    IF EXISTS (
        WITH raices(entidad,id) AS (
            SELECT 'pei', id FROM sisplan.pei
            UNION ALL SELECT 'poa', id FROM sisplan.poa WHERE pei_id IS NULL
        )
        SELECT FROM raices r FULL JOIN sisplan.f0_mapa_historico m
            ON (r.entidad,r.id) = (m.entidad,m.registro_id)
        WHERE r.id IS NULL OR m.registro_id IS NULL
    ) THEN
        RAISE EXCEPTION 'Complete f0_mapa_historico para cada PEI y POA independiente de la instalación. Hay registros sin asignar o asignaciones sin registro.';
    END IF;
END $$;

CREATE TABLE sisplan.institucion (
    id uuid PRIMARY KEY,
    nombre text NOT NULL CHECK (btrim(nombre) <> ''),
    activa boolean NOT NULL DEFAULT true,
    pei_habilitado boolean NOT NULL DEFAULT true,
    poa_habilitado boolean NOT NULL DEFAULT true
);
CREATE TABLE sisplan.dependencia (
    id uuid PRIMARY KEY,
    institucion_id uuid NOT NULL REFERENCES sisplan.institucion(id),
    nombre text NOT NULL CHECK (btrim(nombre) <> ''),
    activa boolean NOT NULL DEFAULT true,
    UNIQUE (institucion_id,id),
    UNIQUE (institucion_id,nombre)
);
CREATE TABLE sisplan.periodo (
    id uuid PRIMARY KEY,
    institucion_id uuid NOT NULL REFERENCES sisplan.institucion(id),
    nombre text NOT NULL CHECK (btrim(nombre) <> ''),
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL CHECK (fecha_fin >= fecha_inicio),
    UNIQUE (institucion_id,nombre)
);
CREATE TABLE sisplan.unidad_medida (
    institucion_id uuid NOT NULL REFERENCES sisplan.institucion(id),
    codigo text NOT NULL CHECK (btrim(codigo) <> ''),
    nombre text NOT NULL CHECK (btrim(nombre) <> ''),
    PRIMARY KEY (institucion_id,codigo)
);
CREATE TABLE sisplan.membresia (
    emisor text NOT NULL CHECK (btrim(emisor) <> ''),
    sujeto text NOT NULL CHECK (btrim(sujeto) <> ''),
    institucion_id uuid NOT NULL REFERENCES sisplan.institucion(id),
    rol text NOT NULL CHECK (rol IN ('SuperAdministrador','AdministradorInstitucional',
        'DirectorPlanificacion','AnalistaPlanificacion','ResponsableArea','DirectorArea','Auditor',
        'MaximaAutoridad','ComiteEstrategico','Planificador')),
    dependencia_id uuid,
    activa boolean NOT NULL DEFAULT true,
    PRIMARY KEY (emisor,sujeto,institucion_id),
    FOREIGN KEY (institucion_id,dependencia_id) REFERENCES sisplan.dependencia(institucion_id,id),
    CHECK ((rol IN ('ResponsableArea','DirectorArea')) = (dependencia_id IS NOT NULL))
);

INSERT INTO sisplan.institucion(id,nombre) SELECT id,nombre FROM sisplan.f0_destino_historico;

-- Las políticas y la migración usan claves compuestas. Una relación tampoco
-- puede cruzar instituciones conociendo el UUID de otro padre.
DO $$
DECLARE
    tabla text;
BEGIN
    FOREACH tabla IN ARRAY ARRAY['pei','pei_version','eje','eje_version','objetivo','indicador',
        'indicador_medicion','pei_transicion','poa','poa_evaluacion'] LOOP
        EXECUTE format('ALTER TABLE sisplan.%I ADD COLUMN institucion_id uuid REFERENCES sisplan.institucion(id)', tabla);
    END LOOP;
END $$;

UPDATE sisplan.pei p SET institucion_id = m.institucion_id
FROM sisplan.f0_mapa_historico m WHERE m.entidad='pei' AND m.registro_id=p.id;
UPDATE sisplan.pei_version h SET institucion_id=p.institucion_id FROM sisplan.pei p WHERE h.pei_id=p.id;
UPDATE sisplan.eje h SET institucion_id=p.institucion_id FROM sisplan.pei_version p WHERE h.pei_version_id=p.id;
UPDATE sisplan.eje_version h SET institucion_id=p.institucion_id FROM sisplan.eje p WHERE h.eje_id=p.id;
UPDATE sisplan.objetivo h SET institucion_id=p.institucion_id FROM sisplan.eje p WHERE h.eje_id=p.id;
UPDATE sisplan.indicador h SET institucion_id=p.institucion_id FROM sisplan.objetivo p WHERE h.objetivo_id=p.id;
UPDATE sisplan.indicador_medicion h SET institucion_id=p.institucion_id FROM sisplan.indicador p WHERE h.indicador_id=p.id;
UPDATE sisplan.pei_transicion h SET institucion_id=p.institucion_id FROM sisplan.pei_version p WHERE h.pei_version_id=p.id;
UPDATE sisplan.poa h SET institucion_id=p.institucion_id FROM sisplan.pei p WHERE h.pei_id=p.id;
UPDATE sisplan.poa h SET institucion_id=m.institucion_id FROM sisplan.f0_mapa_historico m
WHERE m.entidad='poa' AND m.registro_id=h.id AND h.pei_id IS NULL;
UPDATE sisplan.poa_evaluacion h SET institucion_id=p.institucion_id FROM sisplan.poa p WHERE h.poa_id=p.id;

ALTER TABLE sisplan.pei ADD UNIQUE (institucion_id,id);
ALTER TABLE sisplan.pei_version ADD UNIQUE (institucion_id,id),
    ADD FOREIGN KEY (institucion_id,pei_id) REFERENCES sisplan.pei(institucion_id,id);
ALTER TABLE sisplan.eje ADD UNIQUE (institucion_id,id),
    ADD FOREIGN KEY (institucion_id,pei_version_id) REFERENCES sisplan.pei_version(institucion_id,id);
ALTER TABLE sisplan.eje_version
    ADD FOREIGN KEY (institucion_id,eje_id) REFERENCES sisplan.eje(institucion_id,id);
-- La FK previa (eje_id,eje_revision) ya comprueba la revisión exacta.
ALTER TABLE sisplan.objetivo ADD UNIQUE (institucion_id,id),
    ADD FOREIGN KEY (institucion_id,eje_id) REFERENCES sisplan.eje(institucion_id,id);
ALTER TABLE sisplan.indicador ADD UNIQUE (institucion_id,id),
    ADD FOREIGN KEY (institucion_id,objetivo_id) REFERENCES sisplan.objetivo(institucion_id,id);
ALTER TABLE sisplan.indicador_medicion
    ADD FOREIGN KEY (institucion_id,indicador_id) REFERENCES sisplan.indicador(institucion_id,id);
ALTER TABLE sisplan.pei_transicion
    ADD FOREIGN KEY (institucion_id,pei_version_id) REFERENCES sisplan.pei_version(institucion_id,id);
ALTER TABLE sisplan.poa ADD UNIQUE (institucion_id,id),
    ADD COLUMN dependencia_id uuid,
    ADD FOREIGN KEY (institucion_id,pei_id) REFERENCES sisplan.pei(institucion_id,id),
    ADD FOREIGN KEY (institucion_id,dependencia_id) REFERENCES sisplan.dependencia(institucion_id,id);
ALTER TABLE sisplan.poa_evaluacion
    ADD FOREIGN KEY (institucion_id,poa_id) REFERENCES sisplan.poa(institucion_id,id);

DROP INDEX sisplan.poa_independiente_anual;
CREATE UNIQUE INDEX poa_independiente_anual ON sisplan.poa(institucion_id,anio) WHERE pei_id IS NULL;

CREATE FUNCTION sisplan.institucion_actual() RETURNS uuid
LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('sisplan.institucion_id',true),'')::uuid
$$;
-- La función solo lee el actor autenticado. El propietario puede leer metadatos
-- de membresía sin recursión entre sus propias políticas.
CREATE FUNCTION sisplan.membresias_actuales()
RETURNS TABLE (institucion_id uuid, rol text, dependencia_id uuid)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $$
    SELECT m.institucion_id,m.rol,m.dependencia_id
    FROM sisplan.membresia m JOIN sisplan.institucion i ON i.id=m.institucion_id
    LEFT JOIN sisplan.dependencia d ON (d.institucion_id,d.id)=(m.institucion_id,m.dependencia_id)
    WHERE m.emisor=nullif(current_setting('sisplan.emisor',true),'')
      AND m.sujeto=nullif(current_setting('sisplan.sujeto',true),'')
      AND m.activa AND i.activa AND (m.dependencia_id IS NULL OR d.activa)
$$;
CREATE FUNCTION sisplan.dependencia_actual() RETURNS uuid
LANGUAGE sql STABLE AS $$
    SELECT m.dependencia_id FROM sisplan.membresias_actuales() m WHERE m.institucion_id=sisplan.institucion_actual()
$$;
ALTER TABLE sisplan.poa ALTER COLUMN dependencia_id SET DEFAULT sisplan.dependencia_actual();

ALTER TABLE sisplan.membresia ENABLE ROW LEVEL SECURITY;
CREATE POLICY identidad_propia ON sisplan.membresia FOR SELECT USING (
    emisor=current_setting('sisplan.emisor',true) AND sujeto=current_setting('sisplan.sujeto',true)
    AND institucion_id IN (SELECT m.institucion_id FROM sisplan.membresias_actuales() m)
);
ALTER TABLE sisplan.institucion ENABLE ROW LEVEL SECURITY;
CREATE POLICY instituciones_propias ON sisplan.institucion FOR SELECT USING (
    id IN (SELECT m.institucion_id FROM sisplan.membresias_actuales() m)
);
ALTER TABLE sisplan.dependencia ENABLE ROW LEVEL SECURITY;
CREATE POLICY dependencias_propias ON sisplan.dependencia FOR SELECT USING (
    institucion_id=sisplan.institucion_actual() AND EXISTS (
        SELECT FROM sisplan.membresias_actuales() m WHERE m.institucion_id=dependencia.institucion_id
          AND (m.dependencia_id IS NULL OR dependencia.id=m.dependencia_id)
    )
);
ALTER TABLE sisplan.periodo ENABLE ROW LEVEL SECURITY;
CREATE POLICY periodos_propios ON sisplan.periodo FOR SELECT USING (
    institucion_id=sisplan.institucion_actual() AND institucion_id IN (SELECT m.institucion_id FROM sisplan.membresias_actuales() m)
);
ALTER TABLE sisplan.unidad_medida ENABLE ROW LEVEL SECURITY;
CREATE POLICY unidades_propias ON sisplan.unidad_medida FOR SELECT USING (
    institucion_id=sisplan.institucion_actual() AND institucion_id IN (SELECT m.institucion_id FROM sisplan.membresias_actuales() m)
);

-- La API comprueba la acción y el estado de negocio. PostgreSQL delimita además
-- la institución, los módulos, el alcance por área y las escrituras de cada familia.
CREATE FUNCTION sisplan.rol_modulo(modulo text) RETURNS text
LANGUAGE sql STABLE SET search_path=pg_catalog AS $$
    SELECT m.rol FROM sisplan.membresias_actuales() m JOIN sisplan.institucion i ON i.id=m.institucion_id
    WHERE i.id=sisplan.institucion_actual()
      AND CASE modulo WHEN 'pei' THEN i.pei_habilitado WHEN 'poa' THEN i.poa_habilitado ELSE false END
$$;

DO $$
DECLARE
    tabla text;
    modulo text;
    alcance text;
    lectura text;
    escritura text;
    escritores text[];
BEGIN
    FOREACH tabla IN ARRAY ARRAY['pei','pei_version','eje','eje_version','objetivo','indicador',
        'indicador_medicion','pei_transicion','poa','poa_evaluacion'] LOOP
        modulo := CASE WHEN tabla IN ('poa','poa_evaluacion') THEN 'poa' ELSE 'pei' END;
        alcance := 'institucion_id=(SELECT sisplan.institucion_actual())';
        IF tabla='poa' THEN
            alcance := alcance || ' AND ((SELECT sisplan.dependencia_actual()) IS NULL OR dependencia_id=(SELECT sisplan.dependencia_actual()))';
        ELSIF tabla='poa_evaluacion' THEN
            alcance := alcance || ' AND EXISTS (SELECT FROM sisplan.poa p WHERE p.id=poa_evaluacion.poa_id)';
        END IF;
        lectura := format('(%s) AND (SELECT sisplan.rol_modulo(%L)) IS NOT NULL', alcance,modulo);
        escritores := CASE
            WHEN modulo='poa' THEN ARRAY['DirectorPlanificacion','AnalistaPlanificacion','Planificador','ResponsableArea']
            ELSE ARRAY['AnalistaPlanificacion','Planificador'] END;
        escritura := format('(%s) AND (SELECT sisplan.rol_modulo(%L))=ANY(%L::text[])', alcance,modulo,escritores);

        EXECUTE format('ALTER TABLE sisplan.%I ALTER COLUMN institucion_id SET NOT NULL, ALTER COLUMN institucion_id SET DEFAULT sisplan.institucion_actual()',tabla);
        EXECUTE format('ALTER TABLE sisplan.%I ENABLE ROW LEVEL SECURITY',tabla);
        EXECUTE format('ALTER TABLE sisplan.%I FORCE ROW LEVEL SECURITY',tabla);
        EXECUTE format('CREATE POLICY lectura ON sisplan.%I FOR SELECT TO sisplan_app USING (%s)',tabla,lectura);
        IF tabla='pei_transicion' THEN
            -- Su política de inserción se define abajo; el historial es de solo anexado.
            GRANT SELECT,INSERT ON sisplan.pei_transicion TO sisplan_app;
            CONTINUE;
        END IF;
        EXECUTE format('CREATE POLICY alta ON sisplan.%I FOR INSERT TO sisplan_app WITH CHECK (%s)',tabla,escritura);
        IF tabla='eje_version' THEN
            -- Editar un eje agrega una revisión, igual que registrar una transición agrega un evento.
            GRANT SELECT,INSERT ON sisplan.eje_version TO sisplan_app;
            CONTINUE;
        END IF;
        -- Las transiciones bloquean la raíz PEI y su versión antes de cambiar el estado.
        IF tabla IN ('pei','pei_version') THEN
            escritura := format('(%s) AND (SELECT sisplan.rol_modulo(''pei'')) IN (''DirectorPlanificacion'',''AnalistaPlanificacion'',''Planificador'',''MaximaAutoridad'',''ComiteEstrategico'')',alcance);
        END IF;
        EXECUTE format('CREATE POLICY cambio ON sisplan.%I FOR UPDATE TO sisplan_app USING (%s) WITH CHECK (%s)',tabla,escritura,escritura);
        -- Los repositorios actuales no eliminan planes ni su estructura histórica.
        EXECUTE format('GRANT SELECT,INSERT,UPDATE ON sisplan.%I TO sisplan_app',tabla);
    END LOOP;
END $$;

CREATE POLICY alta ON sisplan.pei_transicion FOR INSERT TO sisplan_app WITH CHECK (
    institucion_id=sisplan.institucion_actual()
    AND emisor=current_setting('sisplan.emisor',true) AND sujeto=current_setting('sisplan.sujeto',true)
    AND CASE (SELECT sisplan.rol_modulo('pei'))
        WHEN 'AnalistaPlanificacion' THEN accion IN ('enviar','corregir')
        WHEN 'Planificador' THEN accion IN ('enviar','corregir')
        WHEN 'DirectorPlanificacion' THEN accion IN ('revisar','devolver','publicar')
        WHEN 'ComiteEstrategico' THEN accion IN ('validar_comite','devolver')
        WHEN 'MaximaAutoridad' THEN accion IN ('aprobar','devolver')
        ELSE false END
);

-- Se revisan las asignaciones nuevas o modificadas. Una baja posterior del
-- módulo PEI o de la dependencia conserva el vínculo y los campos autónomos del POA.
-- El bloqueo compartido impide que el catálogo cambie durante la asignación.
CREATE FUNCTION sisplan.validar_asignacion_poa() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
DECLARE
    cambia_vinculo boolean;
    asigna_dependencia boolean;
BEGIN
    IF TG_OP='INSERT' THEN
        cambia_vinculo := NEW.pei_id IS NOT NULL;
        asigna_dependencia := NEW.dependencia_id IS NOT NULL;
    ELSE
        cambia_vinculo := (NEW.pei_id,NEW.pei_version_id) IS DISTINCT FROM (OLD.pei_id,OLD.pei_version_id);
        asigna_dependencia := NEW.dependencia_id IS NOT NULL
            AND (NEW.institucion_id,NEW.dependencia_id) IS DISTINCT FROM (OLD.institucion_id,OLD.dependencia_id);
    END IF;
    IF cambia_vinculo THEN
        PERFORM 1 FROM sisplan.institucion WHERE id=NEW.institucion_id AND pei_habilitado AND activa FOR SHARE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'La institución no tiene habilitado el vínculo con un PEI.' USING ERRCODE='42501';
        END IF;
    END IF;
    IF asigna_dependencia THEN
        PERFORM 1 FROM sisplan.dependencia
        WHERE institucion_id=NEW.institucion_id AND id=NEW.dependencia_id AND activa FOR SHARE;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'La dependencia de destino no está disponible en la institución.' USING ERRCODE='42501';
        END IF;
    END IF;
    RETURN NULL;
END $$;
CREATE TRIGGER validar_asignacion AFTER INSERT OR UPDATE OF pei_id,pei_version_id,dependencia_id,institucion_id
ON sisplan.poa FOR EACH ROW EXECUTE FUNCTION sisplan.validar_asignacion_poa();
REVOKE ALL ON FUNCTION sisplan.validar_asignacion_poa() FROM PUBLIC;

CREATE TABLE sisplan.auditoria (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    institucion_id uuid NOT NULL,
    emisor text NOT NULL,
    sujeto text NOT NULL,
    fecha timestamptz NOT NULL DEFAULT clock_timestamp(),
    entidad text NOT NULL,
    referencia text NOT NULL,
    accion text NOT NULL CHECK (accion IN ('INSERT','UPDATE','DELETE')),
    anterior jsonb,
    nuevo jsonb
);
CREATE INDEX auditoria_institucion_fecha ON sisplan.auditoria(institucion_id,fecha DESC,id DESC);
ALTER TABLE sisplan.auditoria ENABLE ROW LEVEL SECURITY;
CREATE POLICY historial_institucional ON sisplan.auditoria FOR SELECT TO sisplan_app USING (
    institucion_id=sisplan.institucion_actual() AND EXISTS (
        SELECT FROM sisplan.membresias_actuales() m WHERE m.institucion_id=auditoria.institucion_id
        AND m.rol IN ('Auditor','AdministradorInstitucional')
    )
);

-- El login no puede escribir, borrar ni truncar el historial. El disparador
-- inserta con los permisos de su propietario y comparte la transacción original.
CREATE FUNCTION sisplan.registrar_cambio() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $$
DECLARE
    previo jsonb := CASE WHEN TG_OP<>'INSERT' THEN to_jsonb(OLD) END;
    posterior jsonb := CASE WHEN TG_OP<>'DELETE' THEN to_jsonb(NEW) END;
    fila jsonb := coalesce(posterior,previo);
    institucion uuid;
    referencia text;
    emisor text := nullif(current_setting('sisplan.emisor',true),'');
    sujeto text := nullif(current_setting('sisplan.sujeto',true),'');
BEGIN
    IF previo IS NOT DISTINCT FROM posterior THEN RETURN NULL; END IF;
    institucion := (CASE WHEN TG_TABLE_NAME='institucion' THEN fila->>'id' ELSE fila->>'institucion_id' END)::uuid;
    referencia := CASE TG_TABLE_NAME
        WHEN 'eje_version' THEN concat(fila->>'eje_id','/',fila->>'revision')
        WHEN 'pei_transicion' THEN concat(fila->>'pei_version_id','/',fila->>'revision')
        WHEN 'membresia' THEN jsonb_build_array(fila->>'emisor',fila->>'sujeto')::text
        WHEN 'unidad_medida' THEN fila->>'codigo'
        ELSE fila->>'id' END;
    -- La provisión y la administración SQL también conservan el actor de base.
    IF emisor IS NULL OR sujeto IS NULL THEN
        emisor := 'postgresql';
        sujeto := session_user;
    END IF;
    INSERT INTO sisplan.auditoria(institucion_id,emisor,sujeto,entidad,referencia,accion,anterior,nuevo)
    VALUES (institucion,emisor,sujeto,TG_TABLE_NAME,referencia,TG_OP,previo,posterior);
    RETURN NULL;
END $$;
DO $$
DECLARE
    tabla text;
BEGIN
    FOREACH tabla IN ARRAY ARRAY['institucion','dependencia','periodo','membresia','unidad_medida','pei','pei_version',
        'eje','eje_version','objetivo','indicador','indicador_medicion','pei_transicion','poa','poa_evaluacion'] LOOP
        EXECUTE format('CREATE TRIGGER auditar_cambios AFTER INSERT OR UPDATE OR DELETE ON sisplan.%I FOR EACH ROW EXECUTE FUNCTION sisplan.registrar_cambio()',tabla);
    END LOOP;
END $$;

GRANT USAGE ON SCHEMA sisplan TO sisplan_app;
GRANT SELECT ON sisplan.institucion,sisplan.dependencia,sisplan.periodo,sisplan.unidad_medida,sisplan.membresia,sisplan.auditoria TO sisplan_app;
REVOKE ALL ON FUNCTION sisplan.registrar_cambio() FROM PUBLIC;
REVOKE ALL ON FUNCTION sisplan.membresias_actuales() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sisplan.membresias_actuales() TO sisplan_app;
