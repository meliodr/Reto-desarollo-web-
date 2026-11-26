#!/usr/bin/env bash
# Verificación sobre contenedores y volúmenes propios. Ejecutar desde la raíz del repositorio.
set -euo pipefail
umask 077
temporal=$(mktemp -d)
export COMPOSE_PROJECT_NAME="sisplan-f0-$(openssl rand -hex 6)"
export SISPLAN_DB_PASSWORD="$(openssl rand -hex 20)"
export SISPLAN_APP_DB_PASSWORD="$(openssl rand -hex 20)"
export SISPLAN_KEYCLOAK_PASSWORD="$(openssl rand -hex 20)"
export SISPLAN_F0_PASSWORD="$(openssl rand -hex 20)"
export SISPLAN_F0_TOTP_SEED="$(openssl rand -hex 16)"
export ASPNETCORE_ENVIRONMENT=Development
export Sisplan__Oidc__Url=http://127.0.0.1:8080
api_pid=
web_pid=
limpiar() {
  if [[ -n "$web_pid" ]]; then kill "$web_pid" 2>/dev/null || true; fi
  if [[ -n "$api_pid" ]]; then kill "$api_pid" 2>/dev/null || true; fi
  docker compose --profile institucional down --volumes >/dev/null
  rm -rf -- "$temporal"
}
trap limpiar EXIT

sql() {
  docker compose exec -T postgres psql -X -v ON_ERROR_STOP=1 -U sisplan -d "$@"
}
cadena_migracion() {
  echo "Host=127.0.0.1;Port=55432;Database=$1;Username=sisplan;Password=$SISPLAN_DB_PASSWORD"
}
export ConnectionStrings__SisplanMigraciones="$(cadena_migracion sisplan)"
export ConnectionStrings__Sisplan="Host=127.0.0.1;Port=55432;Database=sisplan;Username=sisplan_app;Password=$SISPLAN_APP_DB_PASSWORD;Maximum Pool Size=1"

docker compose --profile institucional up -d --wait --wait-timeout 180
dotnet build backend/Sisplan.Api.csproj -c Release
api_dll=backend/bin/Release/net10.0/Sisplan.Api.dll
dotnet "$api_dll" --migrate
dotnet "$api_dll" --migrate
sql sisplan < infra/postgres/fixtures-f0.sql
sql sisplan < tests/f0_db.sql

# Reproducir el ejecutable que existía antes de 0008 con su registro real de migraciones.
git archive b8d1f4bbaa87b7814d4dab9aa2fc359872bbb1b0 backend | tar -x -C "$temporal"
sql sisplan -c 'CREATE DATABASE sisplan_anterior'
historica=$(cadena_migracion sisplan_anterior)
dotnet build "$temporal/backend/Sisplan.Api.csproj" -c Release
ConnectionStrings__Sisplan="$historica" dotnet "$temporal/backend/bin/Release/net10.0/Sisplan.Api.dll" --migrate
sql sisplan_anterior < infra/postgres/preparar-historicos.sql
sql sisplan_anterior < tests/f0_historicos.sql
ConnectionStrings__SisplanMigraciones="$historica" dotnet "$api_dll" --migrate
ConnectionStrings__SisplanMigraciones="$historica" dotnet "$api_dll" --migrate
sql sisplan_anterior < tests/f0_verificar_historicos.sql

curl -fsS --retry 80 --retry-connrefused --retry-delay 2 --max-time 8 "$Sisplan__Oidc__Url/realms/sisplan/.well-known/openid-configuration" -o /dev/null
python3 infra/keycloak/f0.py usuarios
dotnet "$api_dll" >"$temporal/api.log" 2>&1 &
api_pid=$!
if ! curl -fsS --retry 10 --retry-connrefused --retry-delay 1 http://127.0.0.1:5080/api/configuracion -o /dev/null; then
  cat "$temporal/api.log"
  exit 1
fi
tokens="$temporal/tokens.json"
(cd frontend && exec node node_modules/@angular/cli/bin/ng.js serve --host 127.0.0.1 --proxy-config proxy.conf.json) >"$temporal/angular.log" 2>&1 &
web_pid=$!
if ! curl -fsS --retry 60 --retry-connrefused --retry-delay 1 http://127.0.0.1:4200/ -o /dev/null; then
  cat "$temporal/angular.log"
  exit 1
fi
python3 infra/keycloak/f0.py tokens --salida "$tokens"
python3 tests/f0_smoke.py --tokens "$tokens" --modo ambos
python3 tests/f0_interfaz.py --capturas "${RUNNER_TEMP:-$temporal}/sisplan-f0-capturas"
sql sisplan -c "UPDATE sisplan.institucion SET poa_habilitado=false WHERE id='00000000-0000-4000-8000-000000000001'"
python3 tests/f0_smoke.py --tokens "$tokens" --modo solo-pei
sql sisplan -c "UPDATE sisplan.institucion SET pei_habilitado=false, poa_habilitado=true WHERE id='00000000-0000-4000-8000-000000000001'"
python3 tests/f0_smoke.py --tokens "$tokens" --modo solo-poa
sql sisplan -c "UPDATE sisplan.institucion SET pei_habilitado=true WHERE id='00000000-0000-4000-8000-000000000001'"

python3 - "$tokens" <<'PY'
import json
import os
import subprocess
import sys

with open(sys.argv[1]) as source:
    tokens = json.load(source)
env = os.environ.copy()
for name, key in {"ANALISTA": "04", "DIRECTOR": "03", "COMITE": "09", "AUTORIDAD": "08", "AUDITOR": "07"}.items():
    env[f"SISPLAN_{name}_TOKEN"] = tokens[key]
for test in ["pei", "ejes", "objetivos", "indicadores", "flujo"]:
    subprocess.run([sys.executable, f"tests/{test}_smoke.py"], env=env, check=True)
PY
