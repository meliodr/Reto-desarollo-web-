"""Verifica la publicación autónoma y la conservación de los vínculos explícitos a POA."""

import argparse
import json
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from f0_smoke import A, B
from flujo_smoke import call


def run(base, tokens, comprobar_poa=False):
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

    def concurrent_post(path, who, data, tag):
        req = Request(base + path, data=json.dumps(data).encode(),
                      method="POST", headers={"Authorization": f"Bearer {tokens[who]}",
                                              "X-Institucion-Id": A, "If-Match": tag,
                                              "Content-Type": "application/json"})
        try:
            response = urlopen(req, timeout=15)
        except HTTPError as error:
            response = error
        with response:
            return response.status, response.headers.get("ETag")

    def submit_for_approval(pei, tag):
        path = f"/pei-versiones/{pei['versionId']}"
        tag = transition(path, "enviar", "04", tag)
        tag = transition(path, "revisar", "03", tag)
        return path, transition(path, "validar_comite", "09", tag)

    def approve(pei, tag):
        path, tag = submit_for_approval(pei, tag)
        return path, transition(path, "aprobar", "08", tag)

    def load(pei):
        return api("04", f"/peis/{pei['id']}/catalogo-digemaps", 201, "POST", {})

    session, _ = api("04", "/sesion", 200)
    if comprobar_poa:
        assert session["instituciones"][0]["modulos"] == {"pei": True, "poa": True}
        comprobar_vinculos_poa(api, create, approve, load, transition, concurrent_post)
        return
    assert session["instituciones"][0]["modulos"] == {"pei": True, "poa": False}
    api("04", "/poas", 403)

    # Los años límite que admite la API también deben poder leerse desde PostgreSQL.
    for start, end, kind in (
        ("0001-01-01", "0005-01-01", "extraordinario"),
        ("9996-12-31", "9999-12-31", "ordinario"),
    ):
        boundary, _ = create(start, end, kind)
        boundary_path = f"/pei-versiones/{boundary['versionId']}"
        assert api("07", boundary_path, 200)[0] == boundary
        for invalid in ("0000-12-31", "9999-02-29", "10000-01-01"):
            errors, _ = api("04", "/peis", 422, "POST", dict(boundary, fechaFin=invalid))
            assert "fechaFin" in errors["errors"]

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
    catalog_axes = api("04", axes, 200)[0]
    axis = catalog_axes[0]
    # Excluir un objetivo de un eje activo y los objetivos de otro eje inactivo.
    retired_axis = next(e for e in catalog_axes[1:] if e["nombre"] != "Conducción")
    retired_axis_path = f"{axes}/{retired_axis['id']}"
    retired_goals = api("04", retired_axis_path + "/objetivos", 200)[0]
    _, retired_headers = api("04", retired_axis_path, 200)
    api("04", retired_axis_path, 200, "PUT", dict(retired_axis, activo=False), retired_headers["ETag"])
    expected_pending = 18 - len(retired_goals)
    axis_path = f"{axes}/{axis['id']}"
    _, axis_headers_for_original = api("04", axis_path, 200)
    axis_history, _ = api("04", axis_path + "/versiones", 200)
    inactive, axis_headers = api("04", axes, 201, "POST", {"nombre": "Eje histórico inactivo"})
    api("04", f"{axes}/{inactive['id']}", 200, "PUT", {
        "nombre": inactive["nombre"], "activo": False, "revision": inactive["revision"],
    }, axis_headers["ETag"])
    goals = f"{axes}/{axis['id']}/objetivos"
    catalog_goals = api("04", goals, 200)[0]
    seeded_goal = catalog_goals[0]
    retired_goal_path = f"{goals}/{catalog_goals[1]['id']}"
    _, retired_headers = api("04", retired_goal_path, 200)
    api("04", retired_goal_path + "/estado", 200, "PATCH", {"activo": False}, retired_headers["ETag"])
    goal_path = f"{goals}/{seeded_goal['id']}"
    _, seeded_goal_headers = api("04", goal_path, 200)
    indicators = f"{goals}/{seeded_goal['id']}/indicadores"
    imported, = api("04", indicators, 200)[0]
    detail = f"{indicators}/{imported['id']}"
    _, indicator_headers = api("04", detail, 200)
    ready, _ = api("04", detail, 200, "PUT", {
        "nombre": imported["nombre"], "formula": "Conteo mensual de expedientes",
        "unidad": "expedientes", "frecuencia": "Mensual", "sentido": "ascendente",
        "meta": "12.5", "metodoCalculo": "valor_directo",
    }, indicator_headers["ETag"])
    assert ready["listoParaMedir"] and ready["lineaBase"] is None
    api("04", f"{detail}/mediciones", 201, "POST", {"periodo": "2170-02-01", "valor": "7.5"})
    legacy_fields = {
        "nombre": "Indicador histórico F0 de prueba", "formula": "Conteo de solicitudes", "unidad": "solicitudes",
        "frecuencia": "Mensual", "sentido": "ascendente", "meta": "80", "metodoCalculo": "valor_directo",
    }
    legacy, _ = api("04", indicators, 201, "POST", legacy_fields)
    legacy_path = f"{indicators}/{legacy['id']}"
    # La base de CI conserva la forma histórica de F0 al insertar este dato.
    legacy, legacy_headers = api("04", legacy_path, 200)
    assert not legacy["listoParaMedir"] and legacy["metodoCalculo"] is None
    risk, risk_headers = api("04", f"{goal_path}/riesgos", 201, "POST", {
        "nombre": "Riesgo de continuidad", "descripcion": "Demora en la atención.",
        "probabilidad": 2, "impacto": 3,
    })
    empty_goal, goal_headers = api("04", goals, 201, "POST", {
        "codigo": "F1-SIN-INDICADOR", "nombre": "Objetivo para validar",
        "descripcion": "Validar la estructura obligatoria.", "resultadoEsperado": "Estructura comprobada.",
        "lineaAccion": "Registrar la prueba de aceptación.",
    })
    goal = f"{goals}/{empty_goal['id']}/estado"
    tag = transition(version, "enviar", "04", tag)
    transition(version, "validar_comite", "09", tag, 403)
    with ThreadPoolExecutor(max_workers=2) as pool:
        attempts = [pool.submit(concurrent_post, version + "/transiciones", "03", {"accion": "revisar"}, tag)
                    for _ in range(2)]
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
    version, tag = submit_for_approval(plan, tag)
    pending, _ = api("08", version + "/transiciones", 409, "POST", {"accion": "aprobar"}, tag)
    assert pending["title"] == "Hay indicadores manuales pendientes de configuración."
    tag = transition(version, "devolver", "08", tag, reason="Configuración pendiente de un indicador manual.")
    configured, _ = api("04", legacy_path, 200, "PUT", legacy_fields, legacy_headers["ETag"])
    assert configured["listoParaMedir"]
    tag = transition(version, "corregir", "04", tag)
    corrected_options, _ = api("04", version + "/transiciones", 200)
    assert "indicadoresDigemapsPendientes" not in corrected_options
    version, approved = approve(plan, tag)
    publication_options, _ = api("03", version + "/transiciones", 200)
    assert publication_options["acciones"] == ["publicar"]
    assert publication_options["indicadoresDigemapsPendientes"] == expected_pending
    authority_options, _ = api("08", version + "/transiciones", 200)
    assert "indicadoresDigemapsPendientes" not in authority_options
    transition(version, "publicar", "08", approved, 403)
    published_tag = transition(version, "publicar", "03", approved)
    published_options, _ = api("03", version + "/transiciones", 200)
    assert "indicadoresDigemapsPendientes" not in published_options
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
        "validar_comite", "devolver", "corregir", "enviar", "revisar", "validar_comite",
        "aprobar", "publicar",
    ]
    observed = history[3]
    assert observed["motivo"] == reason and observed["estadoAnterior"] == "en_revision"
    assert observed["estadoNuevo"] == "observado" and observed["sujeto"].endswith("000000000008")
    assert history[-1]["estadoNuevo"] == "publicado" and history[-1]["registradaEn"] == first["publicadoEn"]
    assert history[-1]["revision"] == first["revision"] and history[-1]["sujeto"].endswith("000000000003")
    assert all(event["emisor"] and datetime.fromisoformat(event["registradaEn"]).tzinfo for event in history)

    map_path = f"/peis/{plan['id']}/mapa"
    original_query = map_path + f"?versionId={plan['versionId']}"
    original_map = api("07", original_query, 200)[0]
    original_ids = {axis["id"] for axis in original_map["ejes"]}
    original_indicators = [i for axis in original_map["ejes"] for goal in axis["objetivos"] for i in goal["indicadores"]]
    assert len(original_indicators) == 21
    assert {i["id"] for i in original_indicators if i["listoParaMedir"]} == {ready["id"], configured["id"]}
    revision_data = {"motivo": "Ajustar la visión del plan oficial."}
    api("07", versions, 403, "POST", revision_data, published_tag)
    api("04", versions, 428, "POST", revision_data)
    api("04", versions, 422, "POST", {"motivo": " "}, published_tag)
    api("04", versions, 412, "POST", revision_data, approved)
    successor, head = api("04", versions, 201, "POST", revision_data, published_tag)
    revised_tag = head["ETag"]
    api("04", versions, 409, "POST", revision_data, published_tag)
    assert successor["estado"] == "borrador" and successor["versionOrigenId"] == plan["versionId"]
    assert successor["fechaInicio"] == official["fechaInicio"] and successor["fechaFin"] == official["fechaFin"]
    assert api("07", f"/peis/{plan['id']}", 200)[0] == successor
    assert official in api("07", "/peis?oficiales=true", 200)[0]
    assert successor not in api("07", "/peis?oficiales=true", 200)[0]
    assert api("07", version, 200)[0] == official
    assert api("07", original_query, 200)[0] == original_map
    assert transition(version, "publicar", "03", published_tag) == published_tag
    transition(version, "publicar", "03", approved, 412)
    transition(version, "publicar", "08", published_tag, 403)
    assert api("07", version + "/transiciones", 200)[0]["acciones"] == []
    assert api("07", axis_path, 200)[0] == axis
    assert api("07", axis_path + "/versiones", 200)[0] == axis_history
    assert api("07", goal_path, 200)[0] == seeded_goal
    risk_path = f"{goal_path}/riesgos/{risk['id']}"
    assert api("07", risk_path, 200)[0] == risk
    api("04", axis_path, 404, "PUT", dict(axis, activo=False), axis_headers_for_original["ETag"])
    api("04", goal_path + "/estado", 404, "PATCH", {"activo": False}, seeded_goal_headers["ETag"])
    api("04", risk_path, 404, "PUT", dict(risk, probabilidad=3), risk_headers["ETag"])
    assert api("07", detail, 200)[0] == ready
    assert [m["valor"] for m in api("07", f"{detail}/mediciones", 200)[0]] == ["7.5"]
    api("04", f"{detail}/mediciones", 404, "POST", {"periodo": "2170-03-01", "valor": "8"})
    assert api("07", versions + "?fecha=2171-01-01", 200)[0] == [first]
    second = api("07", versions, 200)[0][0]
    assert second["numero"] == 2 and second["publicadoEn"] is None
    assert second["versionOrigenId"] == first["versionId"] and second["motivoRevision"] == revision_data["motivo"]
    assert second["creadaPorSujeto"].endswith("000000000004") and second["creadaPorEmisor"] == history[0]["emisor"]

    copied_map = api("07", map_path, 200)[0]
    assert copied_map["versionId"] == successor["versionId"]
    assert len(copied_map["ejes"]) == len(original_ids) and original_ids.isdisjoint(e["id"] for e in copied_map["ejes"])
    copied_goals = [goal for axis in copied_map["ejes"] for goal in axis["objetivos"]]
    copied_risk, = [r for goal in copied_goals for r in goal["riesgos"]]
    assert copied_risk["nombre"] == risk["nombre"] and copied_risk["id"] != risk["id"]
    copied_indicators = [i for goal in copied_goals for i in goal["indicadores"]]
    assert len(copied_indicators) == 21
    copied_axis, copied_goal, copied_ready = next(
        (axis, goal, i) for axis in copied_map["ejes"] for goal in axis["objetivos"]
        for i in goal["indicadores"] if i["nombre"] == ready["nombre"])
    assert sum(i["listoParaMedir"] for i in copied_indicators) == 2 and copied_ready["id"] != ready["id"]
    fields = ("nombre", "formula", "unidad", "frecuencia", "sentido", "meta", "metodoCalculo")
    assert all(copied_ready[key] == ready[key] for key in fields)
    copied_configured, = [i for i in copied_indicators if i["nombre"] == configured["nombre"]]
    assert copied_configured["id"] != configured["id"] and copied_configured["listoParaMedir"]
    assert all(copied_configured[key] == configured[key] for key in fields)
    assert api("07", f"{axes}/{copied_axis['id']}/objetivos/{copied_goal['id']}/indicadores/{copied_ready['id']}/mediciones",
               200)[0] == []

    revised_path = f"/pei-versiones/{successor['versionId']}"
    api("04", revised_path, 422, "PATCH", dict(successor, fechaInicio="2170-02-01"), revised_tag)
    # El PEI editado tendrá una descripción reconocible mientras que el original permanece visible por su ID.
    revised, head = api("04", revised_path, 200, "PATCH", dict(successor,
                       nombre="Plan estratégico revisado de 2170", vision="Atender con calidad sostenible."), revised_tag)
    revised_path, revised_approved = approve(revised, head["ETag"])
    transition(revised_path, "publicar", "03", revised_approved)
    assert api("07", revised_path, 200)[0] in api("07", "/peis?oficiales=true", 200)[0]
    assert official not in api("07", "/peis?oficiales=true", 200)[0]
    assert transition(version, "publicar", "03", published_tag) == published_tag
    assert api("07", f"{detail}/mediciones", 200)[0][0]["valor"] == "7.5"
    assert api("07", version + "/historial", 200)[0] == history
    dated, _ = api("07", versions + "?fecha=2171-01-01", 200)
    assert [candidate["numero"] for candidate in dated] == [2, 1]

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
        attempts = [pool.submit(concurrent_post, path + "/transiciones", "03", {"accion": "publicar"}, tag)
                    for path, tag in concurrent]
        statuses = sorted(attempt.result()[0] for attempt in attempts)
    assert statuses == [200, 409], statuses
    api("04", "/poas", 403)
    print("F1.3/F1.4: estructura, observaciones, sucesora, historial, concurrencia, vigencia y publicación autónoma verificados.")


def comprobar_vinculos_poa(api, create, approve, load, transition, concurrent_post):
    plan, tag = create("2260-01-01", "2263-12-31")
    path = f"/peis/{plan['id']}"
    load(plan)
    first, tag = approve(plan, tag)
    published_tag = transition(first, "publicar", "03", tag)
    assert api("07", path + "/poas", 200)[0] == []
    original, _ = api("04", "/poas", 201, "POST", {"anio": 2260, "peiId": plan["id"]})
    assert (original["peiVersionId"], original["peiNombre"]) == (plan["versionId"], plan["nombre"])

    revisions = path + "/versiones"
    request = {"motivo": "Actualizar el PEI después del primer POA vinculado."}
    with ThreadPoolExecutor(max_workers=2) as pool:
        attempts = [pool.submit(concurrent_post, revisions, "04", request, published_tag) for _ in range(2)]
        result = sorted(attempt.result() for attempt in attempts)
    assert [status for status, _ in result] == [201, 409], result
    draft, official = api("07", revisions, 200)[0]
    assert draft["numero"] == 2 and official["versionId"] == plan["versionId"]
    current = f"/pei-versiones/{draft['versionId']}"
    draft, headers = api("07", current, 200)
    different_name = "PEI con programación posterior"
    revised, headers = api("04", current, 200, "PATCH", dict(draft, nombre=different_name), headers["ETag"])
    during_draft, _ = api("04", "/poas", 201, "POST", {"anio": 2261, "peiId": plan["id"]})
    assert (during_draft["peiVersionId"], during_draft["peiNombre"]) == (plan["versionId"], plan["nombre"])

    current, tag = approve(revised, headers["ETag"])
    revised_tag = transition(current, "publicar", "03", tag)
    assert transition(first, "publicar", "03", published_tag) == published_tag
    assert transition(current, "publicar", "03", revised_tag) == revised_tag
    after_publication, _ = api("04", "/poas", 201, "POST", {
        "anio": 2262, "peiId": plan["id"], "presupuestoReferencia": "0",
    })
    assert (after_publication["peiVersionId"], after_publication["peiNombre"]) == (revised["versionId"], different_name)
    assert after_publication["presupuestoReferencia"] == "0.00"
    linked = {r["id"]: (r["peiVersionId"], r["peiNombre"], r["presupuestoReferencia"])
              for r in api("07", "/poas", 200)[0] if r["peiId"] == plan["id"]}
    assert linked == {
        original["id"]: (plan["versionId"], plan["nombre"], None),
        during_draft["id"]: (plan["versionId"], plan["nombre"], None),
        after_publication["id"]: (revised["versionId"], different_name, "0.00"),
    }
    assert {r["id"] for r in api("07", path + "/poas", 200)[0]} == set(linked)
    print("F1.4: los POA anteriores conservaron su versión y los nuevos usaron la oficial vigente.")

    manual, tag = create("2264-01-01", "2267-12-31")
    axes = f"/peis/{manual['id']}/ejes"
    axis, _ = api("04", axes, 201, "POST", {"nombre": "Eje sin indicadores DIGEMAPS"})
    goals = f"{axes}/{axis['id']}/objetivos"
    goal, _ = api("04", goals, 201, "POST", {
        "codigo": "MAN-01", "nombre": "Resolver solicitudes", "descripcion": "Mejorar el tiempo de atención.",
        "resultadoEsperado": "Responder las solicitudes a tiempo.", "lineaAccion": "Registrar el tiempo de atención.",
    })
    api("04", f"{goals}/{goal['id']}/indicadores", 201, "POST", {
        "nombre": "Solicitudes respondidas", "formula": "Conteo de solicitudes", "unidad": "solicitudes",
        "frecuencia": "Mensual", "sentido": "ascendente", "meta": "10", "metodoCalculo": "valor_directo",
    })
    path, tag = approve(manual, tag)
    options, _ = api("03", path + "/transiciones", 200)
    assert options["acciones"] == ["publicar"] and options["indicadoresDigemapsPendientes"] == 0
    transition(path, "publicar", "03", tag)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--tokens", type=Path, required=True)
    parser.add_argument("--comprobar-poa", action="store_true",
                        help="Comprueba vínculos explícitos entre versiones con ambos módulos habilitados.")
    args = parser.parse_args()
    if not args.base.startswith(("http://127.0.0.1:", "http://localhost:")):
        parser.error("Usa la API de prueba en loopback.")
    run(args.base.rstrip("/"), json.loads(args.tokens.read_text()), args.comprobar_poa)


if __name__ == "__main__":
    main()
