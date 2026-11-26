"""Verifica la publicación e historial del PEI con el módulo POA deshabilitado."""

import argparse
import json
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from f0_smoke import A, B
from flujo_smoke import call


def run(base, tokens):
    def api(account, path, status, method="GET", data=None, etag=None, institution=A):
        return call(base, path, status, method, data, tokens[account], etag, institution)

    def create(start, end, kind="ordinario"):
        result, headers = api("04", "/peis", 201, "POST", {
            "nombre": f"Plan estratégico {start} a {end}", "tipo": kind,
            "fechaInicio": start, "fechaFin": end, "mision": "Atender a las personas.",
            "vision": "Mejorar el servicio.", "valores": "Responsabilidad.",
        })
        return result, headers["ETag"]

    def transition(version, action, who, tag, expected=200, reason=None):
        return api(who, version + "/transiciones", expected, "POST",
                   {"accion": action, "motivo": reason}, tag)[1].get("ETag")

    def concurrent_transition(version, action, tag):
        req = Request(base + version + "/transiciones", data=json.dumps({"accion": action}).encode(),
                      method="POST", headers={"Authorization": f"Bearer {tokens['03']}",
                                              "X-Institucion-Id": A, "If-Match": tag,
                                              "Content-Type": "application/json"})
        try:
            response = urlopen(req, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            return response.status, response.headers.get("ETag")

    def approve(pei, tag):
        path = f"/pei-versiones/{pei['versionId']}"
        tag = transition(path, "enviar", "04", tag)
        tag = transition(path, "revisar", "03", tag)
        tag = transition(path, "validar_comite", "09", tag)
        return path, transition(path, "aprobar", "08", tag)

    def load(pei):
        return api("04", f"/peis/{pei['id']}/catalogo-digemaps", 201, "POST", {})

    session, _ = api("04", "/sesion", 200)
    assert session["instituciones"][0]["modulos"] == {"pei": True, "poa": False}
    api("04", "/poas", 403)

    plan, tag = create("2170-01-01", "2173-12-31")
    version = f"/pei-versiones/{plan['versionId']}"
    versions = f"/peis/{plan['id']}/versiones"
    api("04", f"/peis/{plan['id']}/poas", 403)
    assert api("07", version + "/historial", 200)[0] == []
    assert api("07", versions + "?fecha=2171-01-01", 200)[0] == []
    api("07", versions + "?fecha=2171-02-29", 400)
    for route in (version, version + "/historial", versions):
        api("10", route, 404, institution=B)

    load(plan)
    axes = f"/peis/{plan['id']}/ejes"
    axis = api("04", axes, 200)[0][0]
    inactive, axis_headers = api("04", axes, 201, "POST", {"nombre": "Eje histórico inactivo"})
    api("04", f"{axes}/{inactive['id']}", 200, "PUT", {
        "nombre": inactive["nombre"], "activo": False, "revision": inactive["revision"],
    }, axis_headers["ETag"])
    goals = f"{axes}/{axis['id']}/objetivos"
    empty_goal, goal_headers = api("04", goals, 201, "POST", {
        "codigo": "F1-SIN-INDICADOR", "nombre": "Objetivo para validar",
        "descripcion": "Validar la estructura obligatoria.", "resultadoEsperado": "Estructura comprobada.",
        "lineaAccion": "Registrar la prueba de aceptación.",
    })
    goal = f"{goals}/{empty_goal['id']}/estado"
    tag = transition(version, "enviar", "04", tag)
    transition(version, "validar_comite", "09", tag, 403)
    with ThreadPoolExecutor(max_workers=2) as pool:
        attempts = [pool.submit(concurrent_transition, version, "revisar", tag) for _ in range(2)]
        result = sorted(attempt.result() for attempt in attempts)
    assert [status for status, _ in result] == [200, 412], result
    tag = result[0][1]
    tag = transition(version, "validar_comite", "09", tag)
    transition(version, "aprobar", "08", tag, 409)
    reason = "El objetivo de prueba todavía no tiene un indicador."
    tag = transition(version, "devolver", "08", tag, reason=reason)
    api("07", goal, 403, "PATCH", {"activo": False}, goal_headers["ETag"])
    api("04", goal, 200, "PATCH", {"activo": False}, goal_headers["ETag"])
    tag = transition(version, "corregir", "04", tag)
    version, approved = approve(plan, tag)
    transition(version, "publicar", "08", approved, 403)
    published_tag = transition(version, "publicar", "03", approved)
    assert transition(version, "publicar", "03", published_tag) == published_tag
    transition(version, "publicar", "03", approved, 412)

    official, head = api("07", version, 200)
    assert official["estado"] == "publicado" and official["versionId"] == plan["versionId"]
    assert official == api("07", f"/peis/{plan['id']}", 200)[0] and head["ETag"] == published_tag
    api("04", version, 409, "PATCH", official, published_tag)
    first, = api("07", versions, 200)[0]
    assert first["numero"] == 1 and first["estado"] == "publicado" and first["versionId"] == plan["versionId"]
    assert api("07", versions + "?fecha=2173-12-31", 200)[0] == [first]
    assert api("07", versions + "?fecha=2174-01-01", 200)[0] == []
    assert official in api("07", "/peis", 200)[0]

    history, _ = api("07", version + "/historial", 200)
    assert [event["accion"] for event in history] == [
        "enviar", "revisar", "validar_comite", "devolver", "corregir", "enviar", "revisar",
        "validar_comite", "aprobar", "publicar",
    ]
    observed = history[3]
    assert observed["motivo"] == reason and observed["estadoAnterior"] == "en_revision"
    assert observed["estadoNuevo"] == "observado" and observed["sujeto"].endswith("000000000008")
    assert history[-1]["estadoNuevo"] == "publicado" and history[-1]["registradaEn"] == first["publicadoEn"]
    assert history[-1]["revision"] == first["revision"] and history[-1]["sujeto"].endswith("000000000003")
    assert all(event["emisor"] and datetime.fromisoformat(event["registradaEn"]).tzinfo for event in history)

    coincident, tag = create("2173-12-31", "2177-11-30", "extraordinario")
    load(coincident)
    path, tag = approve(coincident, tag)
    transition(path, "publicar", "03", tag, 409)
    assert api("07", path, 200)[0]["estado"] == "aprobado"

    concurrent = []
    for _ in range(2):
        candidate, tag = create("2174-01-01", "2177-12-31")
        load(candidate)
        concurrent.append(approve(candidate, tag))
    with ThreadPoolExecutor(max_workers=2) as pool:
        attempts = [pool.submit(concurrent_transition, path, "publicar", tag) for path, tag in concurrent]
        statuses = sorted(attempt.result()[0] for attempt in attempts)
    assert statuses == [200, 409], statuses
    api("04", "/poas", 403)
    print("F1.3/F1.4: estructura, historial, observaciones, revisión concurrente, vigencia y publicación autónoma verificados.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--tokens", type=Path, required=True)
    args = parser.parse_args()
    if not args.base.startswith(("http://127.0.0.1:", "http://localhost:")):
        parser.error("Usa la API de prueba en loopback.")
    run(args.base.rstrip("/"), json.loads(args.tokens.read_text()))


if __name__ == "__main__":
    main()
