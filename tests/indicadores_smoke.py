"""Comprueba RF-004 contra la API local con PostgreSQL de prueba."""

import argparse
from uuid import uuid4

from pei_smoke import request


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    args = parser.parse_args()
    base = args.base.rstrip("/")
    draft = {
        "nombre": f"PEI de indicadores {uuid4().hex[:8]}",
        "tipo": "ordinario",
        "fechaInicio": "2026-02-01",
        "fechaFin": "2029-09-30",
        "mision": "Misión de prueba.",
        "vision": "Visión de prueba.",
        "valores": "Transparencia y responsabilidad.",
    }
    pei, _ = request(base, "/peis", 201, "POST", draft)
    other_pei, _ = request(base, "/peis", 201, "POST", draft)
    ejes = f"/peis/{pei['id']}/ejes"
    eje, eje_headers = request(base, ejes, 201, "POST", {"nombre": "Servicios digitales"})
    other_eje, _ = request(base, ejes, 201, "POST", {"nombre": "Gestión de procesos"})
    eje_path = f"{ejes}/{eje['id']}"
    objective_fields = {
        "codigo": "OE-01",
        "nombre": "Digitalizar trámites",
        "descripcion": "Migrar los trámites frecuentes.",
        "resultadoEsperado": "Trámites en línea.",
        "lineaAccion": "Publicar el catálogo digital.",
    }
    objective, _ = request(base, f"{eje_path}/objetivos", 201, "POST", objective_fields)
    other_objective, _ = request(
        base, f"{eje_path}/objetivos", 201, "POST", dict(objective_fields, codigo="OE-02")
    )
    path = f"{eje_path}/objetivos/{objective['id']}/indicadores"
    wrong_path = (
        f"/peis/{other_pei['id']}/ejes/{eje['id']}/objetivos/{objective['id']}/indicadores"
    )
    wrong_axis = f"{ejes}/{other_eje['id']}/objetivos/{objective['id']}/indicadores"
    assert request(base, path, 200)[0] == []
    fields = {
        "nombre": "Trámites digitalizados",
        "formula": "(Trámites en línea / total de trámites) × 100",
        "unidad": "%",
        "lineaBase": "0",
        "meta": "999999999999.0003",
        "frecuencia": "Cuatrimestral",
    }
    errors, _ = request(base, path, 422, "POST", dict(fields, frecuencia=" ", meta=1.00001))
    assert {"frecuencia", "meta"} <= errors["errors"].keys()
    request(base, wrong_path, 404)
    request(base, wrong_path, 404, "POST", fields)
    request(base, wrong_axis, 404, "POST", fields)
    indicator, _ = request(base, path, 201, "POST", fields)
    assert indicator["id"]
    assert all(indicator[key] == value for key, value in fields.items())
    assert request(base, path, 200)[0] == [indicator]

    measurements = f"{path}/{indicator['id']}/mediciones"
    wrong_objective = (
        f"{eje_path}/objetivos/{other_objective['id']}/indicadores/{indicator['id']}/mediciones"
    )
    assert request(base, measurements, 200)[0] == []
    measurement = {"periodo": "2026-02-01", "valor": "999999999999.0003"}
    request(base, wrong_objective, 404)
    request(base, wrong_objective, 404, "POST", measurement)
    for date in ("2026-01-31", "2029-10-01"):
        errors, _ = request(base, measurements, 422, "POST", dict(measurement, periodo=date))
        assert "periodo" in errors["errors"]
    first, _ = request(base, measurements, 201, "POST", measurement)
    assert first["id"]
    assert all(first[key] == value for key, value in measurement.items())
    request(base, measurements, 409, "POST", measurement)
    _, headers = request(base, f"/peis/{pei['id']}", 200)
    errors, _ = request(base, f"/pei-versiones/{pei['versionId']}", 422, "PATCH",
                        dict(draft, fechaInicio="2026-02-02"), headers["ETag"])
    assert set(errors["errors"]) == {"fechaInicio"}
    second, _ = request(
        base, measurements, 201, "POST", {"periodo": "2029-09-30", "valor": "92.125"}
    )
    assert request(base, measurements, 200)[0] == [second, first]

    request(base, eje_path, 200, "PUT", {
        "nombre": eje["nombre"], "descripcion": eje["descripcion"],
        "activo": False, "revision": eje["revision"],
    }, eje_headers["ETag"])
    request(base, path, 409, "POST", fields)
    request(base, measurements, 409, "POST", dict(measurement, periodo="2027-02-01"))
    assert request(base, path, 200)[0] == [indicator]
    assert request(base, measurements, 200)[0] == [second, first]
    print("Indicadores: seis campos, mediciones fechadas, vínculos, duplicados e historial verificados.")


if __name__ == "__main__":
    main()
