"""Aceptación de la configuración y medición de indicadores en un PEI de demostración."""

import argparse
import json
from pathlib import Path

from flujo_smoke import call

HORIZONTE = "00000000-0000-4000-8000-000000000001"
BAHIA = "00000000-0000-4000-8000-000000000002"


def check_f1_indicadores(api):
    demo, _ = api("04", "/peis", 201, "POST", {
        "nombre": "PEI de configuración de indicadores",
        "tipo": "ordinario",
        "fechaInicio": "2145-01-01",
        "fechaFin": "2148-12-31",
        "mision": "Atender solicitudes.",
        "vision": "Mejorar la atención.",
        "valores": "Servicio."
    })
    path = f"/peis/{demo['id']}"
    api("04", f"{path}/catalogo-digemaps", 201, "POST", {})
    axes, _ = api("04", f"{path}/ejes", 200)
    objectives = f"{path}/ejes/{axes[0]['id']}/objetivos"
    goals, _ = api("04", objectives, 200)
    indicators = f"{objectives}/{goals[0]['id']}/indicadores"
    pending, = api("04", indicators, 200)[0]
    assert pending["listoParaMedir"] is False
    assert all(pending[field] is None for field in (
        "formula", "unidad", "lineaBase", "meta", "frecuencia", "sentido", "metodoCalculo"
    ))
    detail = f"{indicators}/{pending['id']}"
    response, headers = api("04", detail, 200)
    assert response == pending and response["revision"] == 1
    tag = headers["ETag"]

    measurement = {"periodo": "2146-01-01", "valor": "7.50"}
    api("04", f"{detail}/mediciones", 409, "POST", measurement)
    assert api("04", f"{detail}/mediciones", 200)[0] == []
    fields = {
        "nombre": pending["nombre"],
        "formula": "Conteo mensual validado",
        "unidad": "expedientes",
        "lineaBase": None,
        "meta": "12.50",
        "frecuencia": "Mensual",
        "sentido": "ascendente",
        "metodoCalculo": "valor_directo",
    }
    api("07", detail, 403, "PUT", fields, tag)
    api("10", detail, 404, "GET", institution=BAHIA)
    api("10", detail, 404, "PUT", fields, tag, BAHIA)
    api("04", detail, 428, "PUT", fields)
    wrong, _ = api("04", detail, 422, "PUT",
                   dict(fields, metodoCalculo="variacion_absoluta"), tag)
    assert set(wrong["errors"]) == {"lineaBase"}
    wrong, _ = api("04", detail, 422, "PUT",
                   dict(fields, metodoCalculo="variacion_porcentual", lineaBase=0), tag)
    assert set(wrong["errors"]) == {"lineaBase"}
    wrong, _ = api("04", detail, 422, "PUT",
                   dict(fields, metodoCalculo="desconocido", sentido="impredecible"), tag)
    assert set(wrong["errors"]) == {"metodoCalculo", "sentido"}

    configured, new_headers = api("04", detail, 200, "PUT", fields, tag)
    assert configured["id"] == pending["id"]
    assert configured["listoParaMedir"] is True and configured["revision"] == 2
    assert configured["lineaBase"] is None and configured["meta"] == "12.5"
    api("04", detail, 412, "PUT", fields, tag)
    assert api("04", detail, 200)[0] == configured
    valid, _ = api("04", f"{detail}/mediciones", 201, "POST", measurement)
    assert valid["valor"] == "7.5"

    with_baseline, latest = api("04", detail, 200, "PUT",
                                dict(fields, metodoCalculo="variacion_porcentual", lineaBase="5"),
                                new_headers["ETag"])
    assert with_baseline["revision"] == 3 and with_baseline["listoParaMedir"] is True
    assert with_baseline["lineaBase"] == "5"

    objective = f"{objectives}/{goals[0]['id']}"
    _, objective_headers = api("04", objective, 200)
    api("04", f"{objective}/estado", 200, "PATCH", {"activo": False}, objective_headers["ETag"])
    api("04", indicators, 409, "POST", fields)
    api("04", detail, 409, "PUT", fields, latest["ETag"])
    api("04", f"{detail}/mediciones", 409, "POST", dict(measurement, periodo="2146-02-01"))
    assert api("04", f"{detail}/mediciones", 200)[0] == [valid]
    print("F1.2: configuración DIGEMAPS, rechazos, ETag y medición de un indicador listo comprobados.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--tokens", type=Path, required=True)
    args = parser.parse_args()
    if not args.base.startswith(("http://127.0.0.1:", "http://localhost:")):
        parser.error("Usa la API desechable de prueba en loopback.")
    tokens = json.loads(args.tokens.read_text())

    def api(account, path, status, method="GET", data=None, tag=None, institution=HORIZONTE):
        return call(args.base.rstrip("/"), path, status, method, data, tokens[account], tag, institution)

    check_f1_indicadores(api)


if __name__ == "__main__":
    main()
