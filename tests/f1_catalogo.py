"""Coteja el catálogo DIGEMAPS y prueba su integridad durante la carga HTTP."""
import argparse
import json
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from xml.etree import ElementTree
from zipfile import ZipFile

from flujo_smoke import call

CATALOGO = Path(__file__).resolve().parents[1] / "backend" / "Catalogos" / "digemaps.json"
PLAN_FALLO = "PEI de prueba con falla transaccional DIGEMAPS"
RESUMEN = {"ejes": 6, "objetivos": 20, "indicadores": 20}


def cotejar_excel(archivo, catalogo):
    with ZipFile(archivo) as libro:
        shared = ElementTree.fromstring(libro.read("xl/sharedStrings.xml"))
        strings = ["".join(t.text or "" for t in celda.iterfind(".//{*}t")) for celda in shared]
        hoja = ElementTree.fromstring(libro.read("xl/worksheets/sheet1.xml"))
        celdas = {c.get("r"): strings[int(v.text)] for c in hoja.iterfind(".//{*}c")
                  if c.get("t") == "s" and (v := c.find("{*}v")) is not None}
    datos = {g["codigo"]: (e["nombre"], g) for e in catalogo for g in e["objetivos"]}
    assert len(datos) == 20
    eje = None
    for fila in range(4, 24):
        eje = celdas.get(f"A{fila}", eje)
        esperado, g = datos[f"DIG-{fila:02}"]
        registrado = [esperado, g["descripcion"], g["resultadoEsperado"], g["indicador"], g["lineaAccion"]]
        assert registrado == [eje, *(celdas[f"{col}{fila}"] for col in "BCDE")], fila
    print("DIGEMAPS: las 20 filas de Hoja1 (A4:E23) coinciden con el catálogo.")


def probar_carga(base, tokens, catalogo):
    analista = tokens["04"]
    auditor = tokens["07"]
    bahia = tokens["10"]
    draft = {"nombre": PLAN_FALLO, "tipo": "extraordinario",
             "fechaInicio": "2270-01-01", "fechaFin": "2274-12-31",
             "mision": "Atender solicitudes.", "vision": "Mejorar la atención.", "valores": "Servicio."}
    call(base, "/peis", 422, "POST", dict(draft, fechaFin="2273-12-31"), analista)
    plan, _ = call(base, "/peis", 201, "POST", draft, analista)
    path = f"/peis/{plan['id']}"
    catalog_path = path + "/catalogo-digemaps"
    call(base, catalog_path, 403, "POST", {}, auditor)
    call(base, catalog_path, 404, "POST", {}, bahia)
    # El disparador de CI falla a mitad de la carga, incluso tras insertar otros ejes.
    call(base, catalog_path, 503, "POST", {}, analista)
    assert call(base, path + "/ejes", 200, token=analista)[0] == []
    assert call(base, path + "/mapa", 200, token=auditor)[0]["ejes"] == []

    # Distinto nombre para retirar la condición de fallo del caso.
    version_path = f"/pei-versiones/{plan['versionId']}"
    tag = call(base, version_path, 200, token=analista)[1]["ETag"]
    call(base, version_path, 200, "PATCH", dict(draft, nombre="PEI DIGEMAPS verificado"), analista, tag)
    assert call(base, catalog_path, 201, "POST", {}, analista)[0] == RESUMEN

    def reintentar(_):
        return call(base, catalog_path, 200, "POST", {}, analista)[0]

    with ThreadPoolExecutor(max_workers=4) as pool:
        assert list(pool.map(reintentar, range(4))) == [RESUMEN] * 4
    mapa, _ = call(base, path + "/mapa", 200, token=auditor)
    ejes = {e["nombre"]: e for e in mapa["ejes"]}
    assert len(mapa["ejes"]) == len(ejes) == 6
    for esperado in catalogo:
        objetivos = ejes[esperado["nombre"]]["objetivos"]
        codigos = {o["codigo"]: o for o in objetivos}
        assert len(objetivos) == len(codigos) == len(esperado["objetivos"])
        for original in esperado["objetivos"]:
            actual = codigos[original["codigo"]]
            for clave in ("nombre", "descripcion", "resultadoEsperado", "lineaAccion"):
                assert actual[clave] == original[clave], (original["codigo"], clave)
            indicador, = actual["indicadores"]
            assert indicador["nombre"] == original["indicador"]
            assert not indicador["listoParaMedir"]
            assert all(indicador[k] is None for k in ("unidad", "meta", "frecuencia", "formula", "lineaBase"))
    manual, _ = call(base, "/peis", 201, "POST", dict(draft, nombre="Estructura manual PEI"), analista)
    manual_path = f"/peis/{manual['id']}"
    call(base, manual_path + "/ejes", 201, "POST", {"nombre": "Eje manual de prueba"}, analista)
    call(base, manual_path + "/catalogo-digemaps", 409, "POST", {}, analista)
    assert len(call(base, manual_path + "/ejes", 200, token=analista)[0]) == 1
    print("F1.1: fidelidad de 20 filas, vigencia extraordinaria, rollback y reintentos de DIGEMAPS comprobados.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--excel", type=Path)
    parser.add_argument("--tokens", type=Path)
    parser.add_argument("--base", default="http://127.0.0.1:5080/api/v1")
    args = parser.parse_args()
    catalogo = json.loads(CATALOGO.read_text())
    if args.excel:
        cotejar_excel(args.excel, catalogo)
    if args.tokens:
        probar_carga(args.base.rstrip("/"), json.loads(args.tokens.read_text()), catalogo)
    if not args.excel and not args.tokens:
        parser.error("Indica la fuente --excel o los --tokens de CI.")
