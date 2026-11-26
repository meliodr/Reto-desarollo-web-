"""Comprueba la consulta del mapa y los riesgos estratégicos con dos instituciones."""

import argparse
import json
from pathlib import Path

from flujo_smoke import call

A = "00000000-0000-4000-8000-000000000001"
B = "00000000-0000-4000-8000-000000000002"


def verify(api):
    draft = {
        "nombre": "PEI de mapa y riesgos",
        "tipo": "ordinario",
        "fechaInicio": "2210-01-01",
        "fechaFin": "2213-12-31",
        "mision": "Mejorar el acceso.",
        "vision": "Servicios accesibles.",
        "valores": "Transparencia.",
    }
    pei, pei_headers = api("04", "/peis", 201, "POST", draft)
    path = f"/peis/{pei['id']}"
    empty = {"peiId": pei["id"], "versionId": pei["versionId"], "estado": "borrador", "ejes": []}
    assert api("04", f"{path}/mapa", 200)[0] == empty

    eje, _ = api("04", f"{path}/ejes", 201, "POST", {"nombre": "Canales de atención", "descripcion": "Ampliar canales."})
    goal = {
        "codigo": "OE-01",
        "nombre": "Dar acceso",
        "descripcion": "Crear nuevas vías para trámites.",
        "resultadoEsperado": "Trámites a distancia.",
        "lineaAccion": "Publicar el catálogo digital.",
    }
    goals = f"{path}/ejes/{eje['id']}/objetivos"
    objetivo, headers = api("04", goals, 201, "POST", goal)
    objetivo_etag = headers["ETag"]
    objetivo_path = f"{goals}/{objetivo['id']}"
    indicador, _ = api("04", f"{objetivo_path}/indicadores", 201, "POST", {
        "nombre": "Trámites habilitados", "formula": "Contar los trámites", "unidad": "Trámite",
        "meta": "20", "frecuencia": "Mensual", "sentido": "ascendente", "metodoCalculo": "valor_directo",
    })
    riesgos = f"{objetivo_path}/riesgos"
    assert api("07", riesgos, 200)[0] == []

    risk = {"nombre": "Interrupción de servicio", "descripcion": "Un canal puede quedar fuera de servicio.",
            "probabilidad": 3, "impacto": 5}
    api("07", riesgos, 403, "POST", risk)
    errors, _ = api("04", riesgos, 422, "POST", dict(risk, probabilidad=0, impacto=6))
    assert {"probabilidad", "impacto"} <= set(errors["errors"])
    api("04", riesgos, 422, "POST", dict(risk, evidenciaUrl="http://example.invalid/acta"))
    for probabilidad, impacto, expected in [(2, 2, "bajo"), (1, 5, "medio"), (3, 5, "alto")]:
        current, head = api("04", riesgos, 201, "POST", dict(risk, probabilidad=probabilidad, impacto=impacto))
        assert current["puntuacion"] == probabilidad * impacto and current["nivel"] == expected
    risk_path = f"{riesgos}/{current['id']}"
    tag = head["ETag"]
    assert api("07", risk_path, 200)[0] == current
    api("04", risk_path, 428, "PUT", risk)
    errors, _ = api("04", risk_path, 422, "PUT", dict(risk, cerrado=True), tag)
    assert {"tratamiento", "accionMitigacion", "responsable", "fechaRevision", "evidenciaUrl"} <= set(errors["errors"])
    closed = dict(risk, cerrado=True, tratamiento="Mitigar", accionMitigacion="Probar canal alterno.",
                  responsable="Operaciones", fechaRevision="2210-01-15", evidenciaUrl="https://example.invalid/acta")
    saved, head = api("04", risk_path, 200, "PUT", closed, tag)
    assert saved["cerrado"] and saved["revision"] == current["revision"] + 1
    api("04", risk_path, 412, "PUT", closed, tag)
    assert api("04", risk_path, 200, "PUT", closed, head["ETag"])[0] == saved

    api("07", f"{objetivo_path}/estado", 403, "PATCH", {"activo": False}, objetivo_etag)
    changed, head = api("04", f"{objetivo_path}/estado", 200, "PATCH", {"activo": False}, objetivo_etag)
    assert not changed["activo"] and changed["revision"] == 2
    api("04", f"{objetivo_path}/estado", 412, "PATCH", {"activo": True}, objetivo_etag)
    api("04", objetivo_path, 409, "PUT", goal, head["ETag"])
    api("04", riesgos, 409, "POST", risk)
    api("07", riesgos, 200)
    reactivado, head = api("04", f"{objetivo_path}/estado", 200, "PATCH", {"activo": True}, head["ETag"])
    tag = head["ETag"]

    _, head = api("04", goals, 201, "POST", dict(goal, codigo="OE-02"))
    ruta_transiciones = f"/pei-versiones/{pei['versionId']}/transiciones"
    _, head = api("04", ruta_transiciones, 200, "POST", {"accion": "enviar"}, pei_headers["ETag"])
    api("04", objetivo_path, 409, "PUT", goal, tag)
    api("03", ruta_transiciones, 200, "POST", {"accion": "devolver", "motivo": "Precisar el resultado esperado."},
        head["ETag"])

    api("07", objetivo_path, 403, "PUT", goal, tag)
    api("04", objetivo_path, 428, "PUT", goal)
    api("04", objetivo_path, 422, "PUT", dict(goal, lineaAccion=" "), tag)
    api("04", objetivo_path, 409, "PUT", dict(goal, codigo="OE-02"), tag)
    corregido = dict(goal, resultadoEsperado="Trámites en línea disponibles.")
    actualizado, head = api("04", objetivo_path, 200, "PUT", corregido, tag)
    assert actualizado["id"] == objetivo["id"] and actualizado["activo"]
    assert actualizado["revision"] == reactivado["revision"] + 1
    api("04", objetivo_path, 412, "PUT", goal, tag)
    repetido, repeat_headers = api("04", objetivo_path, 200, "PUT", corregido, head["ETag"])
    assert repetido == actualizado and repeat_headers["ETag"] == head["ETag"]

    mapa, _ = api("07", f"{path}/mapa?versionId={pei['versionId']}", 200)
    assert mapa["versionId"] == pei["versionId"] and len(mapa["ejes"]) == 1
    axis, = mapa["ejes"]
    objective = next(o for o in axis["objetivos"] if o["id"] == actualizado["id"])
    assert axis["id"] == eje["id"] and axis["activo"]
    assert all(objective[key] == value for key, value in corregido.items()) and objective["activo"]
    assert objective["indicadores"] == [indicador]
    assert saved in objective["riesgos"] and {r["nivel"] for r in objective["riesgos"]} == {"bajo", "medio", "alto"}

    other, _ = api("04", "/peis", 201, "POST", draft)
    api("04", f"{path}/mapa?versionId={other['versionId']}", 404)
    api("04", f"/peis/{other['id']}/ejes/{eje['id']}/versiones", 404)
    api("04", f"/peis/{other['id']}/ejes/{eje['id']}/objetivos/{objetivo['id']}", 404)
    api("10", f"{path}/mapa", 404, institution=B)
    api("10", risk_path, 404, institution=B)
    api("10", riesgos, 404, "POST", risk, institution=B)
    print("F1.2: mapa completo, corrección de objetivos tras devolución, vínculos, riesgos y aislamiento verificados.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tokens", type=Path, required=True)
    args = parser.parse_args()
    tokens = json.loads(args.tokens.read_text())

    def api(account, path, expected, method="GET", body=None, etag=None, institution=A):
        return call("http://127.0.0.1:5080/api/v1", path, expected, method, body, tokens[account], etag, institution)

    verify(api)


if __name__ == "__main__":
    main()
