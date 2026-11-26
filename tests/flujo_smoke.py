"""Verifica la carga estratégica, las aprobaciones y los modos del POA de SRS v2."""

import argparse
import json
import os
from urllib.error import HTTPError
from urllib.request import Request, urlopen
from uuid import uuid4


def call(base, path, expected, method="GET", body=None, token=None, etag=None):
    headers = {"Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if etag:
        headers["If-Match"] = etag
    if body is not None:
        headers["Content-Type"] = "application/json"
    req = Request(base + path, data=json.dumps(body).encode() if body is not None else None,
                  headers=headers, method=method)
    try:
        response = urlopen(req, timeout=10)
    except HTTPError as error:
        response = error
    with response:
        raw = response.read()
        assert response.status == expected, (
            f"{method} {path}: esperaba {expected}, recibí {response.status}: {raw[:500]!r}"
        )
        return json.loads(raw) if raw else None, response.headers


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--year", default=2090, type=int)
    args = parser.parse_args()
    base = args.base.rstrip("/")
    names = ["ANALISTA", "DIRECTOR", "COMITE", "AUTORIDAD", "AUDITOR"]
    tokens = [os.getenv(f"SISPLAN_{name}_TOKEN") for name in names]
    if not all(tokens):
        parser.error("Configura los tokens SISPLAN_ANALISTA_TOKEN, SISPLAN_DIRECTOR_TOKEN, "
                     "SISPLAN_COMITE_TOKEN, SISPLAN_AUTORIDAD_TOKEN y SISPLAN_AUDITOR_TOKEN.")
    analyst, director, committee, authority, auditor = tokens
    year = args.year

    call(base, "/poas", 401)
    call(base, "/poas", 403, "POST", {"anio": year}, auditor)
    poa, _ = call(base, "/poas", 201, "POST", {"anio": year}, analyst)
    assert poa["peiId"] is None and poa["fechaInicio"] == f"{year}-01-01" and poa["estado"] == "borrador"
    call(base, "/poas", 409, "POST", {"anio": year}, analyst)
    call(base, "/poas", 422, "POST", {"anio": 0}, analyst)
    listed, _ = call(base, "/poas", 200, token=auditor)
    assert any(row["id"] == poa["id"] for row in listed)

    data = {
        "nombre": f"PEI de prueba v2 {uuid4().hex[:7]}",
        "tipo": "ordinario",
        "fechaInicio": f"{year}-03-01",
        "fechaFin": f"{year + 3}-11-30",
        "mision": "Servir a la comunidad.",
        "vision": "Aumentar la cobertura de servicios.",
        "valores": "Servicio y responsabilidad.",
    }
    call(base, "/peis", 403, "POST", data, auditor)
    pei, headers = call(base, "/peis", 201, "POST", data, analyst)
    tag = headers["ETag"]
    pei_id = pei["id"]
    load_path = f"/peis/{pei_id}/catalogo-digemaps"
    call(base, load_path, 403, "POST", {}, auditor)
    call(base, f"/peis/{pei_id}/ejes", 403, "POST", {"nombre": "Eje no autorizado"}, auditor)
    call(base, "/poas", 409, "POST", {"anio": year, "peiId": pei_id}, analyst)
    loaded, _ = call(base, load_path, 201, "POST", {}, analyst)
    assert loaded == {"ejes": 6, "objetivos": 20, "indicadores": 20}
    call(base, load_path, 409, "POST", {}, analyst)
    axes, _ = call(base, f"/peis/{pei_id}/ejes", 200, token=analyst)
    assert len(axes) == 6
    axis = next(axis for axis in axes if axis["nombre"] == "Conducción")
    objective_path = f"/peis/{pei_id}/ejes/{axis['id']}/objetivos"
    goals, _ = call(base, objective_path, 200, token=analyst)
    indicators, _ = call(base, f"{objective_path}/{goals[0]['id']}/indicadores", 200, token=analyst)
    assert indicators[0]["nombre"] and indicators[0]["meta"] is None

    path = f"/pei-versiones/{pei['versionId']}/transiciones"

    def action(name, token, etag, status=200, reason=None):
        return call(base, path, status, "POST", {"accion": name, "motivo": reason}, token, etag)

    action("enviar", auditor, tag, 403)
    _, sent = action("enviar", analyst, tag)
    action("revisar", director, tag, 412)
    action("aprobar", authority, sent["ETag"], 403)
    action("devolver", director, sent["ETag"], 422)
    observed, head = action("devolver", director, sent["ETag"], reason="Revisar la descripción.")
    assert observed["estado"] == "observado"
    options, _ = call(base, path, 200, token=analyst)
    assert options == {"acciones": ["corregir"], "motivoDevolucion": "Revisar la descripción."}
    _, edited = call(base, f"/pei-versiones/{pei['versionId']}", 200, "PATCH",
                     dict(data, mision="Servir a todas las comunidades."), analyst, head["ETag"])
    corrected, head = action("corregir", analyst, edited["ETag"])
    assert corrected["estado"] == "corregido"
    _, head = action("enviar", analyst, head["ETag"])
    _, head = action("revisar", director, head["ETag"])
    action("aprobar", authority, head["ETag"], 403)
    _, head = action("validar_comite", committee, head["ETag"])
    _, head = action("aprobar", authority, head["ETag"])
    action("publicar", authority, head["ETag"], 403)
    published, head = action("publicar", director, head["ETag"])
    assert published["estado"] == "publicado"
    _, repeat = action("publicar", director, head["ETag"])
    assert repeat["ETag"] == head["ETag"]
    linked_path = f"/peis/{pei_id}/poas"
    assert call(base, linked_path, 200, token=analyst)[0] == []
    call(base, "/poas", 409, "POST", {"anio": year - 1, "peiId": pei_id}, analyst)
    linked, _ = call(base, "/poas", 201, "POST", {"anio": year, "peiId": pei_id}, analyst)
    assert linked["peiId"] == pei_id and linked["fechaInicio"] == f"{year}-03-01"
    call(base, "/poas", 409, "POST", {"anio": year, "peiId": pei_id}, analyst)
    related, _ = call(base, linked_path, 200, token=analyst)
    assert len(related) == 1 and related[0]["id"] == linked["id"] and related[0]["evaluaciones"] == []
    print("SRS v2: roles, observaciones, corrección, carga DIGEMAPS y POA independiente/vinculado comprobados.")


if __name__ == "__main__":
    main()
