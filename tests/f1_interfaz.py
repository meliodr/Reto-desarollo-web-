"""Consulta las versiones oficiales y el mapa de PEI en Chromium."""

import argparse
import os
from pathlib import Path
from urllib.parse import urlsplit

from playwright.sync_api import Error, Page, expect, sync_playwright

from f0_interfaz import A, B, WEB, cambiar_institucion, captura, entrar_como_multi, etapa_chromium

ORIGINAL = "Plan estratégico 2170-01-01 a 2173-12-31"
REVISADO = "Plan estratégico revisado de 2170"
MOTIVO = "El objetivo de prueba todavía no tiene un indicador."


def corregir_objetivo_observado(pagina: Page):
    with etapa_chromium("abrir el PEI observado y leer la devolución"):
        pagina.get_by_role("combobox", name="Estado del plan", exact=True).select_option("observado")
        pagina.get_by_role("link", name="PEI de mapa y riesgos", exact=True).click()
        expect(pagina.get_by_role("heading", name="Corregir PEI observado", exact=True)).to_be_visible()
        revision = pagina.get_by_role("region", name="Revisión y publicación", exact=True)
        expect(revision.get_by_text("Precisar el resultado esperado.")).to_be_visible()

    panel = pagina.get_by_role("region", name="Objetivos de Canales de atención", exact=True)
    corregido = "Trámites en línea disponibles con confirmación."
    with etapa_chromium("abrir el formulario del objetivo devuelto"):
        pagina.get_by_role("button", name="Ver objetivos de Canales de atención", exact=True).click()
        panel.get_by_role("button", name="Editar objetivo OE-01 · Dar acceso", exact=True).click()
        editor = panel.get_by_role("group", name="Editar objetivo", exact=True)
        campo = editor.get_by_role("textbox", name="Resultado esperado", exact=True)
        expect(campo).to_have_value("Trámites en línea disponibles.")
        campo.fill(corregido)

    objetivo = panel.get_by_role("listitem").filter(
        has=pagina.get_by_role("heading", name="OE-01 · Dar acceso", exact=True)
    )
    with etapa_chromium("guardar la corrección del objetivo con el ETag vigente"):
        with pagina.expect_response(lambda r: "/objetivos/" in r.url and r.request.method == "PUT") as escritura:
            panel.get_by_role("button", name="Guardar objetivo", exact=True).click()
        assert escritura.value.ok
        assert escritura.value.request.headers.get("if-match")
        expect(objetivo.get_by_text(corregido, exact=True)).to_be_visible()

    with etapa_chromium("recargar y comprobar que la corrección persiste"):
        pagina.reload()
        pagina.get_by_role("button", name="Ver objetivos de Canales de atención", exact=True).click()
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
    with etapa_chromium("filtrar y abrir la versión oficial más reciente"):
        pagina.get_by_role("link", name="Planes estratégicos", exact=True).click()
        pagina.get_by_label("Estado del plan").select_option("publicado")
        buscar = pagina.get_by_label("Buscar planes por nombre o año")
        buscar.fill("plan sin resultados para F1")
        expect(pagina.get_by_text("No encontramos planes con esos filtros.")).to_be_visible()
        oficial = pagina.get_by_role("link", name=REVISADO, exact=True)
        for anio in ("2170", "2172", "2173"):
            buscar.fill(anio)
            expect(oficial).to_be_visible()
        buscar.fill("2174")
        expect(oficial).to_have_count(0)
        buscar.fill(REVISADO)
        oficial.click()
        expect(pagina.get_by_role("heading", name=REVISADO, exact=True)).to_be_visible()
        expect(pagina.get_by_text("Versión oficial", exact=True)).to_be_visible()
        expect(pagina.get_by_text("Ajustar la visión del plan oficial.")).to_be_visible()
    with etapa_chromium("navegar entre versiones y consultar el mapa histórico"):
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

    with etapa_chromium("verificar el diseño adaptable y volver al plan"):
        for ancho, alto in ((1280, 900), (390, 844)):
            pagina.set_viewport_size({"width": ancho, "height": alto})
            expect(pagina.get_by_role("heading", name=ORIGINAL, exact=True)).to_be_visible()
            assert pagina.evaluate("document.documentElement.scrollWidth <= window.innerWidth + 1"), (
                f"La vista de versión oficial desborda a {ancho} px."
            )
            captura(pagina, carpeta, f"f1-oficial-{ancho}.png")
        pagina.get_by_role("link", name="Volver al plan estratégico").click()
        expect(pagina.get_by_role("heading", name="PEI oficial", exact=True)).to_be_visible()
    cambiar_institucion(pagina, B, "Auditor")
    with etapa_chromium("verificar que Bahía muestra su propio listado de PEI"):
        expect(pagina.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
        expect(pagina.get_by_text(REVISADO, exact=True)).to_have_count(0)
    assert not consultas_poa, consultas_poa
    print("F1.4: historial y publicación oficial comprobados a 1280 y 390 px, con cambio de versión e institución.")
    cambiar_institucion(pagina, A, "AnalistaPlanificacion")
    with etapa_chromium("editar y volver a abrir un objetivo observado"):
        corregir_objetivo_observado(pagina)
    print("F1.2: Chromium corrigió un objetivo tras una devolución y verificó su persistencia.")


def consultar_vinculos_poa(pagina: Page, password: str, semilla: str, carpeta: Path | None):
    session = entrar_como_multi(pagina, password, semilla)
    activa = next(i for i in session["instituciones"] if i["id"] == session["institucionActivaId"])
    assert activa["modulos"] == {"pei": True, "poa": True}
    casos = (
        (2260, "Plan estratégico 2260-01-01 a 2263-12-31", "Publicación histórica"),
        (2262, "PEI con programación posterior", "Versión oficial"),
    )
    destinos = []
    for anio, nombre, estado in casos:
        with etapa_chromium(f"abrir la versión del PEI vinculada al POA {anio}"):
            with pagina.expect_response(lambda r: r.url == WEB + "/api/v1/poas" and r.request.method == "GET") as lista:
                pagina.get_by_role("link", name="Planes operativos", exact=True).click()
            assert lista.value.ok
            vinculo, = [p for p in lista.value.json() if p["anio"] == anio and p["peiNombre"] == nombre]
            fila = pagina.get_by_role("row").filter(has=pagina.get_by_role("cell", name=str(anio), exact=True))
            enlace = fila.get_by_role("link", name=nombre, exact=True)
            assert vinculo["peiId"] and vinculo["peiVersionId"]
            destino = f"/peis/{vinculo['peiId']}/versiones/{vinculo['peiVersionId']}"
            expect(enlace).to_have_attribute("href", destino)
            if anio == 2262:
                expect(fila.get_by_role("cell", name="RD$ 0.00", exact=True)).to_be_visible()
            enlace.click()
            expect(pagina).to_have_url(WEB + destino)
            expect(pagina.get_by_role("heading", name=nombre, exact=True)).to_be_visible()
            expect(pagina.get_by_text(estado, exact=True)).to_be_visible()
            destinos.append(destino)
            captura(pagina, carpeta, f"INT-01-poa-{anio}-version-pei.png")
    assert destinos[0] != destinos[1]
    print("INT-01: cada POA abrió la versión histórica u oficial que tiene vinculada en PostgreSQL.", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capturas", type=Path)
    parser.add_argument("--vinculos-poa", action="store_true",
                        help="Prueba los enlaces a las versiones vinculadas con ambos módulos habilitados.")
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
                    prueba = consultar_vinculos_poa if args.vinculos_poa else consultar_pei_oficial
                    prueba(contexto.new_page(), password, semilla, args.capturas)
    except Error as exc:
        raise SystemExit(f"Falló el recorrido oficial del PEI ({type(exc).__name__}).") from None


if __name__ == "__main__":
    main()
