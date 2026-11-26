"""Consulta las versiones oficiales y el mapa de PEI en Chromium."""

import argparse
import os
from pathlib import Path
from urllib.parse import urlsplit

from playwright.sync_api import Error, Page, expect, sync_playwright

from f0_interfaz import captura, entrar_como_multi

ORIGINAL = "Plan estratégico 2170-01-01 a 2173-12-31"
REVISADO = "Plan estratégico revisado de 2170"
MOTIVO = "El objetivo de prueba todavía no tiene un indicador."


def consultar_pei_oficial(pagina: Page, password: str, semilla: str, carpeta: Path | None):
    session = entrar_como_multi(pagina, password, semilla)
    activa = next(i for i in session["instituciones"] if i["id"] == session["institucionActivaId"])
    assert activa["modulos"] == {"pei": True, "poa": False}
    consultas_poa = []

    def observar(solicitud):
        ruta = urlsplit(solicitud.url).path
        if ruta.startswith("/api/v1/") and "poas" in ruta.split("/"):
            consultas_poa.append(ruta)

    pagina.on("request", observar)
    pagina.get_by_role("link", name="Planes estratégicos", exact=True).click()
    pagina.get_by_label("Estado del plan").select_option("publicado")
    buscar = pagina.get_by_label("Buscar planes por nombre o año")
    buscar.fill("plan sin resultados para F1")
    expect(pagina.get_by_text("No encontramos planes con esos filtros.")).to_be_visible()
    buscar.fill(REVISADO)
    pagina.get_by_role("link", name=REVISADO, exact=True).click()
    expect(pagina.get_by_role("heading", name=REVISADO, exact=True)).to_be_visible()
    expect(pagina.get_by_text("Versión oficial", exact=True)).to_be_visible()
    pagina.get_by_role("link", name="Volver al plan estratégico").click()
    expect(pagina.get_by_role("heading", name="PEI oficial", exact=True)).to_be_visible()
    pagina.get_by_role("button", name="Ver versiones e historial", exact=True).click()
    expect(pagina.get_by_role("link", name="Versión 2 · Publicado", exact=True)).to_be_visible()
    pagina.get_by_role("link", name="Versión 1 · Publicado", exact=True).click()
    expect(pagina.get_by_role("heading", name=ORIGINAL, exact=True)).to_be_visible()
    expect(pagina.get_by_text("Versión oficial", exact=True)).to_be_visible()
    expect(pagina.get_by_text(MOTIVO)).to_be_visible()
    expect(pagina.get_by_text("Publicación oficial", exact=True)).to_be_visible()
    pagina.get_by_role("button", name="Consultar mapa", exact=True).click()
    pagina.get_by_label("Buscar en el mapa").fill("F1-SIN-INDICADOR")
    expect(pagina.get_by_text("Objetivo para validar", exact=True)).to_be_visible()
    expect(pagina.get_by_text("Objetivo inactivo", exact=True)).to_be_visible()

    for ancho, alto in ((1280, 900), (390, 844)):
        pagina.set_viewport_size({"width": ancho, "height": alto})
        expect(pagina.get_by_role("heading", name=ORIGINAL, exact=True)).to_be_visible()
        assert pagina.evaluate("document.documentElement.scrollWidth <= window.innerWidth + 1"), (
            f"La vista de versión oficial desborda a {ancho} px."
        )
        captura(pagina, carpeta, f"f1-oficial-{ancho}.png")
    assert not consultas_poa, consultas_poa
    print("F1.4: Chromium consultó dos versiones, mapa, motivos y publicación oficial a 1280 y 390 px.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capturas", type=Path)
    args = parser.parse_args()
    password = os.environ.get("SISPLAN_F0_PASSWORD")
    semilla = os.environ.get("SISPLAN_F0_TOTP_SEED")
    if not password or not semilla:
        parser.error("Define SISPLAN_F0_PASSWORD y SISPLAN_F0_TOTP_SEED para las cuentas ficticias.")
    try:
        with sync_playwright() as playwright:
            with playwright.chromium.launch() as navegador:
                with navegador.new_context(
                    viewport={"width": 1280, "height": 900}, locale="es-DO"
                ) as contexto:
                    consultar_pei_oficial(contexto.new_page(), password, semilla, args.capturas)
    except Error as exc:
        raise SystemExit(f"Falló el recorrido oficial del PEI ({type(exc).__name__}).") from None


if __name__ == "__main__":
    main()
