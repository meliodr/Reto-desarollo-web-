"""Aceptación F0 por HTTP con tokens reales del realm y datos de infra/postgres/fixtures-f0.sql."""

import argparse
import json
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from pathlib import Path

from flujo_smoke import call

A = "00000000-0000-4000-8000-000000000001"
B = "00000000-0000-4000-8000-000000000002"
AREA_1 = "00000000-0000-4000-8000-000000000101"
AREA_2 = "00000000-0000-4000-8000-000000000102"
INACTIVA = "00000000-0000-4000-8000-000000000103"
ROLES = ["SuperAdministrador", "AdministradorInstitucional", "DirectorPlanificacion",
         "AnalistaPlanificacion", "ResponsableArea", "DirectorArea", "Auditor",
         "MaximaAutoridad", "ComiteEstrategico"]


def check_session(api):
    api(None, "/sesion", 401)
    api("expirado", "/sesion", 401)
    api("sinMembresia", "/peis", 403)
    api("13", "/poas", 403)  # membresía ligada a una dependencia inactiva
    api("04", "/sesion", 400, institution="invalida")
    api("04", "/sesion", 403, institution=B)
    api("10", "/peis", 403, institution=A)
    default, _ = api("14", "/sesion", 200, institution=None)
    assert default["institucionActivaId"] == A
    assert {row["id"]: row["rol"] for row in default["instituciones"]} == {
        A: "AnalistaPlanificacion", B: "Auditor",
    }
    org_a, _ = api("04", "/organizacion", 200)
    org_b, _ = api("10", "/organizacion", 200, institution=B)
    areas_a = {row["id"]: row["activa"] for row in org_a["dependencias"]}
    assert areas_a[AREA_1] and areas_a[AREA_2] and not areas_a[INACTIVA]
    assert set(areas_a).isdisjoint(row["id"] for row in org_b["dependencias"])
    for org in (org_a, org_b):
        assert org["periodos"] and org["unidadesMedida"]
    print("F0-01: identidad, expiración, membresías y organización comprobadas.")


def check_roles(api, year, draft):
    for i, role in enumerate(ROLES, 1):
        who = f"{i:02d}"
        session, _ = api(who, "/sesion", 200)
        membership, = session["instituciones"]
        assert membership["rol"] == role and membership["id"] == A
        api(who, "/peis", 200)
        api(who, "/poas", 200)
        api(who, "/peis", 201 if i == 4 else 403, "POST", draft)
        api(who, "/poas", 201 if i in (3, 4, 5) else 403, "POST", {"anio": year + i})
        api(who, "/auditoria", 200 if i in (2, 7) else 403)
    area_plan, _ = api("05", "/poas", 201, "POST", {"anio": year + 12, "dependenciaId": AREA_1})
    for dep in (AREA_2, INACTIVA):
        api("05", "/poas", 403, "POST", {"anio": year + 13, "dependenciaId": dep})
    other, _ = api("12", "/poas", 201, "POST", {"anio": year + 14})
    first = api("06", "/poas", 200)[0]
    second = api("12", "/poas", 200)[0]
    assert area_plan["id"] in ids(first) and area_plan["id"] not in ids(second)
    assert other["id"] in ids(second) and other["id"] not in ids(first)
    api("14", "/peis", 201, "POST", draft)
    api("14", "/peis", 403, "POST", draft, institution=B)
    print("F0-02: nueve roles, consulta de auditoría y dos dependencias activas comprobados.")


def check_workflow(api, draft):
    pei, head = api("04", "/peis", 201, "POST", draft)
    version = f"/pei-versiones/{pei['versionId']}/transiciones"
    api("04", f"/peis/{pei['id']}/catalogo-digemaps", 201, "POST", {})

    def advance(current, action, owner, allowed):
        data = {"accion": action, "motivo": "Revisar datos de prueba." if action == "devolver" else None}
        for n in range(1, len(ROLES) + 1):
            account = f"{n:02d}"
            available, _ = api(account, version, 200)
            assert set(available["acciones"]) == (set(allowed) if account == owner else set())
            if account != owner:
                api(account, version, 403, "POST", data, current)
        result, head = api(owner, version, 200, "POST", data, current)
        return result["estado"], head["ETag"]

    sequence = [
        ("enviar", "04", ["enviar"]),
        ("devolver", "03", ["revisar", "devolver"]),
        ("corregir", "04", ["corregir"]),
        ("enviar", "04", ["enviar"]),
        ("revisar", "03", ["revisar", "devolver"]),
        ("devolver", "09", ["validar_comite", "devolver"]),
        ("corregir", "04", ["corregir"]),
        ("enviar", "04", ["enviar"]),
        ("revisar", "03", ["revisar", "devolver"]),
        ("validar_comite", "09", ["validar_comite", "devolver"]),
        ("devolver", "08", ["aprobar", "devolver"]),
        ("corregir", "04", ["corregir"]),
        ("enviar", "04", ["enviar"]),
        ("revisar", "03", ["revisar", "devolver"]),
        ("validar_comite", "09", ["validar_comite", "devolver"]),
        ("aprobar", "08", ["aprobar", "devolver"]),
        ("publicar", "03", ["publicar"]),
    ]
    tag = head["ETag"]
    for action, owner, allowed in sequence:
        state, tag = advance(tag, action, owner, allowed)
    assert state == "publicado"
    print("F0-02: matriz de transiciones recorrida con los nueve actores y devoluciones en tres etapas.")


def ids(rows):
    return {row["id"] for row in rows}


def check_isolation(api, year, draft):
    poa_a, _ = api("04", "/poas", 201, "POST", {"anio": year, "presupuestoReferencia": "75.25"})
    poa_b, _ = api("10", "/poas", 201, "POST", {"anio": year}, institution=B)
    assert poa_a["presupuestoReferencia"] == "75.25" and poa_b["presupuestoReferencia"] is None
    for account, inst in (("04", A), ("10", B)):
        api(account, "/poas", 409, "POST", {"anio": year}, institution=inst)

    pei, headers = api("04", "/peis", 201, "POST", draft)
    path = f"/peis/{pei['id']}"
    version = f"/pei-versiones/{pei['versionId']}"
    eje, _ = api("04", path + "/ejes", 201, "POST", {"nombre": "Servicios de prueba"})
    for suffix in ("", "/ejes", "/poas", f"/ejes/{eje['id']}", f"/ejes/{eje['id']}/versiones"):
        api("10", path + suffix, 404, institution=B)
    api("10", path + "/ejes", 404, "POST", {"nombre": "Otro eje"}, institution=B)
    api("10", version, 404, "PATCH", draft, headers["ETag"], B)
    api("10", version + "/transiciones", 404, institution=B)
    api("10", "/poas", 409, "POST", {"anio": year + 15, "peiId": pei["id"]}, institution=B)
    assert len(api("04", path + "/ejes", 200)[0]) == 1
    assert pei["id"] not in ids(api("10", "/peis", 200, institution=B)[0])

    # Con Maximum Pool Size=1 el lote fuerza reutilización de la misma conexión física.
    def read_tenant(inst):
        found = ids(api("14", "/poas", 200, institution=inst)[0])
        visible, hidden = (poa_a, poa_b) if inst == A else (poa_b, poa_a)
        assert visible["id"] in found and hidden["id"] not in found

    with ThreadPoolExecutor(max_workers=4) as pool:
        list(pool.map(read_tenant, [A, B] * 8))

    actor, _ = api("04", "/sesion", 200)
    audit = api("07", "/auditoria", 200)[0]
    assert all(row["institucionId"] == A for row in audit)
    row = next(row for row in audit if row["entidad"] == "poa" and poa_a["id"] in str(row["referencia"]))
    assert row["accion"] == "INSERT" and row["sujeto"] == actor["actor"]["sujeto"]
    assert row["emisor"] == actor["actor"]["emisor"] and datetime.fromisoformat(row["fecha"]).tzinfo
    assert poa_b["id"] not in str(audit)
    print("F0-03 a F0-06: unicidad por institución, aislamiento, reutilización y trazas comprobados.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--tokens", required=True, type=Path,
                        help="JSON privado con claves 01..14, expirado y sinMembresia.")
    parser.add_argument("--modo", choices=("ambos", "solo-pei", "solo-poa"), default="ambos",
                        help="Configuración que el entorno desechable tiene en la institución A.")
    parser.add_argument("--year", type=int, default=2100, help="Año libre seguido de otros quince.")
    args = parser.parse_args()
    tokens = json.loads(args.tokens.read_text())
    if not 1 <= args.year <= 9984:
        parser.error("El año debe dejar libres los quince años siguientes.")
    if not args.base.startswith(("http://127.0.0.1:", "http://localhost:")):
        parser.error("Usa la API de prueba en loopback.")

    def api(account, path, status, method="GET", data=None, etag=None, institution=A):
        return call(args.base.rstrip("/"), path, status, method, data,
                    tokens[account] if account else None, etag, institution)

    peis = args.modo != "solo-poa"
    poas = args.modo != "solo-pei"
    session, _ = api("04", "/sesion", 200)
    assert session["instituciones"][0]["modulos"] == {"pei": peis, "poa": poas}
    api("04", "/peis", 200 if peis else 403)
    api("04", "/poas", 200 if poas else 403)
    draft = {"nombre": "Plan institucional F0", "tipo": "ordinario",
             "fechaInicio": f"{args.year}-01-01", "fechaFin": f"{args.year+3}-12-31",
             "mision": "Atender solicitudes.", "vision": "Mejorar la atención.", "valores": "Servicio."}
    if not peis:
        api("04", "/peis", 403, "POST", draft)
        api("04", "/poas", 201, "POST", {"anio": args.year + 15})
    if not poas:
        api("04", "/poas", 403, "POST", {"anio": args.year + 15})
        api("04", "/peis", 201, "POST", draft)
    print(f"F0-07: modo {args.modo} comprobado.")
    if peis and poas:
        check_session(api)
        check_roles(api, args.year, draft)
        check_isolation(api, args.year, draft)
        check_workflow(api, draft)


if __name__ == "__main__":
    main()
