#!/bin/sh
# También se puede ejecutar en un volumen de desarrollo que ya tenga datos.
(
    set -eu
    : "${SISPLAN_APP_DB_PASSWORD:?Define SISPLAN_APP_DB_PASSWORD}"
    psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<'SQL'
\getenv app_password SISPLAN_APP_DB_PASSWORD
SELECT format('CREATE ROLE sisplan_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', :'app_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'sisplan_app') \gexec
DO $$
BEGIN
    IF EXISTS (
        SELECT FROM pg_roles WHERE rolname = 'sisplan_app'
          AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls OR NOT rolcanlogin)
    ) OR EXISTS (
        SELECT FROM pg_auth_members m JOIN pg_roles r ON r.oid = m.member WHERE r.rolname = 'sisplan_app'
    ) THEN
        RAISE EXCEPTION 'Se requiere un login sisplan_app sin privilegios administrativos ni roles heredados.';
    END IF;
END $$;
SQL
)
