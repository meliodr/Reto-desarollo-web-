"""Comprueba el recorrido PEI contra una API local y una base desechable."""

import argparse
import json
import uuid
from urllib.error import HTTPError
from urllib.request import Request, urlopen


def request(base, path, expected, method="GET", payload=None, etag=None):
    headers = {"Accept": "application/json"}
    body = None
    if payload is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(payload).encode()
    if etag is not None:
        headers["If-Match"] = etag
    req = Request(base + path, data=body, headers=headers, method=method)
    try:
        response = urlopen(req, timeout=10)
    except HTTPError as error:
        response = error
    with response:
        raw = response.read()
        if response.status != expected:
            raise AssertionError(f"{method} {path}: se esperaba {expected}, "
                                 f"se recibió {response.status}: {raw[:500]!r}")
        return (json.loads(raw) if raw else None), response.headers


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    parser.add_argument("--verify-id", help="Comprueba que un borrador editado sigue tras reiniciar la API")
    args = parser.parse_args()
    base = args.base.rstrip("/")

    if args.verify_id:
        pei, _ = request(base, f"/peis/{uuid.UUID(args.verify_id)}", 200)
        assert pei["vision"] == "Visión actualizada por la prueba de integración."
        print("Borrador editado recuperado después del reinicio.")
        return

    draft = {
        "nombre": f"PEI de prueba {uuid.uuid4().hex[:8]}",
        "tipo": "ordinario",
        "fechaInicio": "2026-01-01",
        "fechaFin": "2029-12-31",
        "mision": "Misión de demostración.",
        "vision": "Visión de demostración.",
        "valores": "Transparencia y responsabilidad.",
    }
    error, _ = request(base, "/peis", 422, "POST", dict(draft, fechaFin="2027-12-31"))
    assert error["errors"]
    created, headers = request(base, "/peis", 201, "POST", draft)
    assert headers["ETag"] and created["estado"] == "borrador"
    path = f"/peis/{created['id']}"
    detail, headers = request(base, path, 200)
    assert all(detail[field] == value for field, value in draft.items())
    original_etag = headers["ETag"]
    assert original_etag

    update_path = f"/pei-versiones/{created['versionId']}"
    updated = dict(draft, vision="Visión actualizada por la prueba de integración.")
    request(base, update_path, 428, "PATCH", updated)
    saved, headers = request(base, update_path, 200, "PATCH", updated, original_etag)
    assert saved["vision"] == updated["vision"]
    assert headers["ETag"] and headers["ETag"] != original_etag
    request(base, update_path, 412, "PATCH", draft, original_etag)
    after_conflict, _ = request(base, path, 200)
    assert after_conflict["vision"] == updated["vision"]

    extraordinary = dict(draft, nombre="PEI extraordinario de prueba", tipo="extraordinario",
                         fechaInicio="9995-06-01", fechaFin="9999-12-31")
    extreme, _ = request(base, "/peis", 201, "POST", extraordinary)
    assert extreme["fechaFin"] == "9999-12-31"

    listed, _ = request(base, "/peis", 200)
    assert any(pei["id"] == created["id"] for pei in listed)
    print("Creación, consulta, edición, período, ETag y listado verificados.")
    print(f"ID para comprobación tras reinicio: {created['id']}")


if __name__ == "__main__":
    main()
