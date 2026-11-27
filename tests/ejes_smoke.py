"""Comprueba RF-002 contra una API local con PostgreSQL de prueba."""

import argparse
from concurrent.futures import ThreadPoolExecutor
from uuid import UUID, uuid4

from pei_smoke import request


def verify_history(base, path):
    history, _ = request(base, f"{path}/versiones", 200)
    assert [item["revision"] for item in history] == [3, 2, 1]
    assert [item["activo"] for item in history] == [False, True, True]
    assert history[0]["nombre"] == history[1]["nombre"]
    assert history[2]["nombre"] == "Fortalecimiento institucional"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--verify-ids", help="ID de PEI e ID de eje separados por coma")
    args = parser.parse_args()
    base = args.base.rstrip("/")
    if args.verify_ids:
        pei_id, eje_id = map(UUID, args.verify_ids.split(","))
        verify_history(base, f"/peis/{pei_id}/ejes/{eje_id}")
        print("Historial de ejes recuperado después del reinicio.")
        return

    draft = {
        "nombre": f"PEI de ejes {uuid4().hex[:8]}",
        "tipo": "ordinario",
        "fechaInicio": "2026-01-01",
        "fechaFin": "2029-12-31",
        "mision": "Misión de prueba.",
        "vision": "Visión de prueba.",
        "valores": "Transparencia y responsabilidad.",
    }
    pei, _ = request(base, "/peis", 201, "POST", draft)
    other_pei, _ = request(base, "/peis", 201, "POST", draft)
    collection = f"/peis/{pei['id']}/ejes"
    listed, _ = request(base, collection, 200)
    assert listed == []

    request(base, collection, 422, "POST", {"nombre": "  "})
    created, headers = request(base, collection, 201, "POST", {
        "nombre": "Fortalecimiento institucional",
    })
    assert created["activo"] is True and created["revision"] == 1
    path = f"{collection}/{created['id']}"
    other_path = f"/peis/{other_pei['id']}/ejes/{created['id']}"
    tag = headers["ETag"]
    update = {
        "nombre": "Fortalecimiento y calidad",
        "descripcion": "Servicios institucionales.",
        "activo": True,
        "revision": 1,
    }
    request(base, path, 428, "PUT", update)
    request(base, other_path, 404, "PUT", update, tag)
    request(base, f"{other_path}/versiones", 404)

    def edit(name):
        try:
            return request(base, path, 200, "PUT", dict(update, nombre=name), tag)
        except AssertionError as error:
            if "se recibió 412" not in str(error):
                raise
            return None

    with ThreadPoolExecutor(max_workers=2) as executor:
        results = list(executor.map(edit, (update["nombre"], "Mejora institucional")))
    successes = [result for result in results if result is not None]
    assert len(successes) == 1
    saved, headers = successes[0]
    assert saved["revision"] == 2 and saved["activo"] is True
    request(base, path, 412, "PUT", update, tag)

    inactive, _ = request(base, path, 200, "PUT", dict(
        update, nombre=saved["nombre"], activo=False, revision=2,
    ), headers["ETag"])
    assert inactive["activo"] is False and inactive["revision"] == 3
    listed, _ = request(base, collection, 200)
    assert listed == [inactive]
    verify_history(base, path)
    print("Creación, edición concurrente, inactivación, historial y separación por PEI verificados.")
    print(f"{pei['id']},{created['id']}")


if __name__ == "__main__":
    main()
