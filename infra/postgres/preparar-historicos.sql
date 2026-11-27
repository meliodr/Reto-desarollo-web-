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
