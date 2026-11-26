"""Consulta las versiones oficiales y el mapa de PEI en Chromium."""

import argparse
import os
from pathlib import Path
from urllib.parse import urlsplit

from playwright.sync_api import Error, Page, expect, sync_playwright

from f0_interfaz import A, B, captura, comprobar_rol, entrar_como_multi, sesion

ORIGINAL = "Plan estratégico 2170-01-01 a 2173-12-31"
REVISADO = "Plan estratégico revisado de 2170"
MOTIVO = "El objetivo de prueba todavía no tiene un indicador."


def corregir_objetivo_observado(pagina: Page):
    pagina.get_by_label("Estado del plan").select_option("observado")
    pagina.get_by_role("link", name="PEI de mapa y riesgos", exact=True).click()
    expect(pagina.get_by_role("heading", name="Corregir PEI observado")).to_be_visible()
    expect(pagina.get_by_text("Precisar el resultado esperado.")).to_be_visible()
    pagina.get_by_role("button", name="Ver objetivos de Canales de atención").click()
    objetivo = pagina.get_by_role("listitem").filter(
        has=pagina.get_by_role("heading", name="OE-01 · Dar acceso", exact=True)
    )
    objetivo.get_by_role("button", name="Editar objetivo Dar acceso").click()
    campo = pagina.get_by_role("textbox", name="Resultado esperado", exact=True)
    expect(campo).to_have_value("Trámites en línea disponibles.")
    corregido = "Trámites en línea disponibles con confirmación."
    campo.fill(corregido)
    with pagina.expect_response(lambda r: "/objetivos/" in r.url and r.request.method == "PUT") as escritura:
        pagina.get_by_role("button", name="Guardar objetivo", exact=True).click()
    assert escritura.value.ok
    assert escritura.value.request.headers.get("if-match")
    expect(objetivo.get_by_text(corregido, exact=True)).to_be_visible()
    pagina.reload()
    pagina.get_by_role("button", name="Ver objetivos de Canales de atención").click()
    expect(objetivo.get_by_text(corregido, exact=True)).to_be_visible()


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
    expect(pagina.get_by_text("Ajustar la visión del plan oficial.")).to_be_visible()
    pagina.get_by_role("link", name="Ver versión anterior").click()
    expect(pagina.get_by_role("heading", name=ORIGINAL, exact=True)).to_be_visible()
    expect(pagina.get_by_text("Publicación histórica", exact=True)).to_be_visible()
    expect(pagina.get_by_text(MOTIVO)).to_be_visible()
    pagina.get_by_role("link", name="Ver versión siguiente").click()
    expect(pagina.get_by_role("heading", name=REVISADO, exact=True)).to_be_visible()
    expect(pagina.get_by_text(MOTIVO)).to_have_count(0)
    pagina.get_by_role("link", name="Volver al plan estratégico").click()
    expect(pagina.get_by_role("heading", name="PEI oficial", exact=True)).to_be_visible()
    pagina.get_by_role("button", name="Ver versiones e historial", exact=True).click()
    expect(pagina.get_by_role("link", name="Versión 2 · Publicado", exact=True)).to_be_visible()
    pagina.get_by_role("link", name="Versión 1 · Publicado", exact=True).click()
    expect(pagina.get_by_role("heading", name=ORIGINAL, exact=True)).to_be_visible()
    expect(pagina.get_by_text("Publicación histórica", exact=True)).to_be_visible()
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
    pagina.get_by_role("link", name="Volver al plan estratégico").click()
    expect(pagina.get_by_role("heading", name="PEI oficial", exact=True)).to_be_visible()
    with pagina.expect_response(sesion) as cambio:
        pagina.get_by_role("combobox", name="Institución activa").select_option(B)
    comprobar_rol(cambio.value, B, "Auditor")
    expect(pagina.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
    expect(pagina.get_by_text(REVISADO, exact=True)).to_have_count(0)
    assert not consultas_poa, consultas_poa
    print("F1.4: historial y publicación oficial comprobados a 1280 y 390 px, con cambio de versión e institución.")
    with pagina.expect_response(sesion) as regreso:
        pagina.get_by_role("combobox", name="Institución activa").select_option(A)
    comprobar_rol(regreso.value, A, "AnalistaPlanificacion")
    corregir_objetivo_observado(pagina)
    print("F1.2: Chromium corrigió un objetivo tras una devolución y verificó su persistencia.")


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
