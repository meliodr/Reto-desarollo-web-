"""Comprueba RF-003 contra una API local con PostgreSQL de prueba."""

import argparse
from uuid import uuid4

from pei_smoke import request


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    args = parser.parse_args()
    base = args.base.rstrip("/")
    draft = {
        "nombre": f"PEI de objetivos {uuid4().hex[:8]}",
        "tipo": "ordinario",
        "fechaInicio": "2026-01-01",
        "fechaFin": "2029-12-31",
        "mision": "Misión de prueba.",
        "vision": "Visión de prueba.",
        "valores": "Transparencia y responsabilidad.",
    }
    pei, _ = request(base, "/peis", 201, "POST", draft)
    other, _ = request(base, "/peis", 201, "POST", draft)
    ejes = f"/peis/{pei['id']}/ejes"
    eje, headers = request(base, ejes, 201, "POST", {"nombre": "Servicios digitales"})
    tag = headers["ETag"]
    sibling, _ = request(base, ejes, 201, "POST", {"nombre": "Gestión de procesos"})
    eje_path = f"{ejes}/{eje['id']}"
    path = f"{eje_path}/objetivos"
    wrong_path = f"/peis/{other['id']}/ejes/{eje['id']}/objetivos"
    objective = {
        "codigo": "OE-01",
        "nombre": "Digitalizar la atención",
        "descripcion": "Migrar los trámites más frecuentes.",
        "resultadoEsperado": "Trámites disponibles en línea.",
        "lineaAccion": "Publicar el catálogo digital de servicios.",
    }
    request(base, path, 422, "POST", dict(objective, lineaAccion=" "))
    request(base, wrong_path, 404)
    request(base, wrong_path, 404, "POST", objective)
    saved, headers = request(base, path, 201, "POST", objective)
    detail_path = f"{path}/{saved['id']}"
    assert headers["Location"] == f"/api/v1{detail_path}"
    detail, _ = request(base, detail_path, 200)
    assert detail == saved
    request(base, f"{wrong_path}/{saved['id']}", 404)
    assert all(saved[key] == value for key, value in objective.items())
    request(base, path, 409, "POST", objective)
    request(base, f"{ejes}/{sibling['id']}/objetivos", 201, "POST", objective)
    listed, _ = request(base, path, 200)
    assert listed == [saved]

    request(base, eje_path, 200, "PUT", {
        "nombre": eje["nombre"], "descripcion": eje["descripcion"],
        "activo": False, "revision": eje["revision"],
    }, tag)
    request(base, path, 409, "POST", dict(objective, codigo="OE-02"))
    listed, _ = request(base, path, 200)
    assert listed == [saved]
    print("Objetivos: cinco campos persistidos, código por eje y vínculo histórico verificados.")


if __name__ == "__main__":
    main()
