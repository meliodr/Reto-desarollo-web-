"""Verifica transiciones y generación POA con dos planificadores autenticados."""

import argparse
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
import json
import os
from urllib.error import HTTPError
from urllib.request import Request, urlopen
from zoneinfo import ZoneInfo


def call(base, path, expected, method="GET", body=None, token=None, etag=None):
    headers = {"Accept": "application/json"}
    payload = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        payload = json.dumps(body).encode()
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if etag:
        headers["If-Match"] = etag
    req = Request(base + path, data=payload, headers=headers, method=method)
    try:
        response = urlopen(req, timeout=10)
    except HTTPError as error:
        response = error
    with response:
        raw = response.read()
        statuses = expected if isinstance(expected, tuple) else (expected,)
        assert response.status in statuses, (
            f"{method} {path}: esperaba {expected}, recibí {response.status}: {raw[:500]!r}"
        )
        return (json.loads(raw) if raw else None), response.headers


def create_pei(base, token, name, initial_year):
    body = {
        "nombre": name,
        "tipo": "extraordinario",
        "fechaInicio": f"{initial_year:04d}-09-15",
        "fechaFin": f"{initial_year + 4:04d}-05-12",
        "mision": "Facilitar el acceso a servicios institucionales.",
        "vision": "Un servicio público oportuno y transparente.",
        "valores": "Responsabilidad y transparencia.",
    }
    plan, headers = call(base, "/peis", 201, "POST", body, token)
    assert plan["estado"] == "borrador"
    return plan, headers["ETag"]


def add_structure(base, token, plan):
    ejes = f"/peis/{plan['id']}/ejes"
    eje, _ = call(base, ejes, 201, "POST", {"nombre": "Servicios digitales"}, token)
    objetivos = f"{ejes}/{eje['id']}/objetivos"
    objetivo, _ = call(base, objetivos, 201, "POST", {
        "codigo": "OE-01",
        "nombre": "Reducir tiempos de respuesta",
        "descripcion": "Simplificar los trámites más frecuentes.",
        "resultadoEsperado": "Menor tiempo de respuesta al ciudadano.",
        "lineaAccion": "Automatizar el registro de solicitudes.",
    }, token)
    call(base, f"{objetivos}/{objetivo['id']}/indicadores", 201, "POST", {
        "nombre": "Tiempo de respuesta",
        "formula": "Horas totales / solicitudes respondidas",
        "unidad": "horas",
        "lineaBase": 48,
        "meta": 24,
        "frecuencia": "mensual",
    }, token)


def approve(base, path, first, second, draft_tag):
    submitted, headers = call(base, path, 200, "POST", {"accion": "enviar"}, first, draft_tag)
    assert submitted["estado"] == "en_revision"
    tag = headers["ETag"]
    assert tag != draft_tag
    approved, headers = call(base, path, 200, "POST", {"accion": "aprobar"}, second, tag)
    assert approved["estado"] == "aprobado"
    assert headers["ETag"] != tag
    return headers["ETag"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--year", type=int, default=2090, help="Primer año de los PEI de prueba")
    args = parser.parse_args()
    if not 1 <= args.year <= 9990:
        parser.error("El año inicial debe permitir dos períodos consecutivos de cinco años.")
    first = os.environ.get("SISPLAN_PLANIFICADOR_1_TOKEN")
    second = os.environ.get("SISPLAN_PLANIFICADOR_2_TOKEN")
    other = os.environ.get("SISPLAN_OTRO_ROL_TOKEN")
    if not first or not second:
        parser.error("Configura SISPLAN_PLANIFICADOR_1_TOKEN y SISPLAN_PLANIFICADOR_2_TOKEN.")
    base = args.base.rstrip("/")
    plan, tag = create_pei(base, first, "PEI de prueba para publicación", args.year)
    path = f"/pei-versiones/{plan['versionId']}/transiciones"
    poa_path = f"/peis/{plan['id']}/poas"

    call(base, path, 401, "POST", {"accion": "enviar"}, etag=tag)
    call(base, poa_path, 401)
    if other:
        call(base, path, 403, "POST", {"accion": "enviar"}, other, tag)
        call(base, poa_path, 403, token=other)
    call(base, path, 428, "POST", {"accion": "enviar"}, first)
    call(base, path, 409, "POST", {"accion": "publicar"}, first, tag)
    before, _ = call(base, poa_path, 200, token=first)
    assert before == []

    _, headers = call(base, path, 200, "POST", {"accion": "enviar"}, first, tag)
    submitted_tag = headers["ETag"]
    call(base, path, 412, "POST", {"accion": "aprobar"}, second, tag)
    call(base, path, 403, "POST", {"accion": "aprobar"}, first, submitted_tag)
    call(base, path, 409, "POST", {"accion": "aprobar"}, second, submitted_tag)
    options, _ = call(base, path, 200, token=first)
    assert "aprobar" not in options["acciones"]
    options, _ = call(base, path, 200, token=second)
    assert "aprobar" in options["acciones"]
    call(base, path, 422, "POST", {"accion": "devolver"}, second, submitted_tag)
    returned, headers = call(base, path, 200, "POST", {
        "accion": "devolver", "motivo": "Incluir objetivos e indicadores antes de aprobar.",
    }, second, submitted_tag)
    assert returned["estado"] == "borrador"
    options, _ = call(base, path, 200, token=first)
    assert options["motivoDevolucion"] == "Incluir objetivos e indicadores antes de aprobar."

    add_structure(base, first, plan)
    latest, current = call(base, f"/peis/{plan['id']}", 200, token=first)
    assert latest["estado"] == "borrador"
    approved_tag = approve(base, path, first, second, current["ETag"])
    published, headers = call(base, path, 200, "POST", {"accion": "publicar"}, second, approved_tag)
    assert published["estado"] == "publicado"
    assert headers["ETag"] != approved_tag
    call(base, f"/pei-versiones/{plan['versionId']}", 409, "PATCH", {}, second, headers["ETag"])
    call(base, f"/peis/{plan['id']}/ejes", 409, "POST", {"nombre": "Eje adicional"}, second)
    poas, _ = call(base, poa_path, 200, token=first)
    assert [poa["anio"] for poa in poas] == list(range(args.year, args.year + 5))
    assert poas[0]["fechaInicio"] == f"{args.year:04d}-09-15"
    assert poas[-1]["fechaFin"] == f"{args.year + 4:04d}-05-12"
    assert all(poa["estado"] == "formulacion" for poa in poas)
    today = datetime.now(ZoneInfo("America/Santo_Domingo")).date().isoformat()
    assert all(poa["vigente"] == (poa["fechaInicio"] <= today <= poa["fechaFin"]) for poa in poas)
    assert all([item["cuatrimestre"] for item in poa["evaluaciones"]] == [1, 2, 3] for poa in poas)
    assert [item["estado"] for item in poas[0]["evaluaciones"]] == [
        "fuera_de_vigencia", "fuera_de_vigencia", "generada"
    ]
    assert [item["estado"] for item in poas[-1]["evaluaciones"]] == [
        "generada", "generada", "fuera_de_vigencia"
    ]
    expected_months = [("01-01", "04-30"), ("05-01", "08-31"), ("09-01", "12-31")]
    for poa in poas:
        for period, (start, end) in zip(poa["evaluaciones"], expected_months, strict=True):
            assert period["fechaInicio"] == f"{poa['anio']:04d}-{start}"
            assert period["fechaFin"] == f"{poa['anio']:04d}-{end}"
    _, replay = call(base, path, 200, "POST", {"accion": "publicar"}, second, headers["ETag"])
    assert replay["ETag"] == headers["ETag"]
    after, _ = call(base, poa_path, 200, token=second)
    assert after == poas

    competing, _ = create_pei(base, first, "PEI solapado de prueba", args.year)
    add_structure(base, first, competing)
    _, current = call(base, f"/peis/{competing['id']}", 200, token=first)
    competing_path = f"/pei-versiones/{competing['versionId']}/transiciones"
    competing_approved = approve(base, competing_path, first, second, current["ETag"])
    call(base, competing_path, 409, "POST", {"accion": "publicar"}, second, competing_approved)
    none, _ = call(base, f"/peis/{competing['id']}/poas", 200, token=first)
    assert none == []

    rival_plans = []
    for label in ("A", "B"):
        rival, _ = create_pei(base, first, f"PEI concurrente {label}", args.year + 5)
        add_structure(base, first, rival)
        _, current = call(base, f"/peis/{rival['id']}", 200, token=first)
        rival_path = f"/pei-versiones/{rival['versionId']}/transiciones"
        rival_tag = approve(base, rival_path, first, second, current["ETag"])
        rival_plans.append((rival_path, rival_tag, rival["id"]))

    def publish_rival(plan):
        rival_path, rival_tag, _ = plan
        result, _ = call(base, rival_path, (200, 409), "POST", {"accion": "publicar"}, second, rival_tag)
        return result.get("estado") == "publicado"

    with ThreadPoolExecutor(max_workers=2) as executor:
        won = list(executor.map(publish_rival, rival_plans))
    assert sorted(won) == [False, True]
    for is_published, (_, _, pei_id) in zip(won, rival_plans, strict=True):
        rival_poas, _ = call(base, f"/peis/{pei_id}/poas", 200, token=first)
        assert len(rival_poas) == (5 if is_published else 0)
    print("Verificados: separación de actores, devolución, bloqueo de estado y revisión, cinco POA, cortes oficiales, vigencia, idempotencia y publicación concurrente sin solapamiento.")


if __name__ == "__main__":
    main()
