"""Recorrido en Angular real de una cuenta con roles distintos en dos instituciones."""

import argparse
import os
import sys
from contextlib import contextmanager
from pathlib import Path
from urllib.parse import urlsplit

from playwright.sync_api import Error, Page, Response, expect, sync_playwright

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "infra" / "keycloak"))
from f0 import completar_otp, semilla_cuenta

WEB = "http://127.0.0.1:4200"
KEYCLOAK = "http://127.0.0.1:8080"
A = "00000000-0000-4000-8000-000000000001"
B = "00000000-0000-4000-8000-000000000002"


@contextmanager
def etapa_chromium(descripcion: str):
    try:
        yield
    except (Error, AssertionError) as error:
        # El diagnóstico omite el registro de Playwright: puede contener URLs de OIDC.
        mensaje = str(error).lower()
        tipo = (
            "control ambiguo" if "strict mode violation" in mensaje
            else "tiempo de espera" if "timeout" in mensaje or "timed out" in mensaje
            else type(error).__name__
        )
        raise SystemExit(f"Chromium falló al {descripcion} ({tipo}).") from None


def sesion(response: Response) -> bool:
    return response.url == WEB + "/api/v1/sesion" and response.request.method == "GET"


def comprobar_rol(response: Response, institucion: str, rol: str):
    assert response.ok, "La API rechazó la consulta de sesión."
    data = response.json()
    assert data["institucionActivaId"] == institucion, "Institución activa inesperada."
    assert any(item["id"] == institucion and item["rol"] == rol for item in data["instituciones"]), (
        "La cuenta no recibió el rol previsto para la institución activa."
    )


def captura(page: Page, directory: Path | None, name: str):
    if directory:
        directory.mkdir(parents=True, exist_ok=True)
        page.locator(".app-shell").screenshot(path=directory / name, animations="disabled")


def cambiar_institucion(page: Page, institucion: str, rol: str):
    selector = page.get_by_role("combobox", name="Institución activa", exact=True)
    with etapa_chromium("seleccionar la institución y verificar su membresía"):
        expect(selector).to_be_enabled()
        with page.expect_response(
            lambda r: sesion(r) and r.request.headers.get("x-institucion-id") == institucion
        ) as cambio:
            selector.select_option(value=institucion)
        comprobar_rol(cambio.value, institucion, rol)
    with etapa_chromium("confirmar la institución activa en el selector"):
        expect(selector).to_have_value(institucion)
        expect(selector).to_be_enabled()


def entrar_como_multi(page: Page, password: str, semilla: str):
    page.goto(WEB + "/institucion")
    entrar = page.get_by_role("button", name="Iniciar sesión", exact=True)
    entrar.click()
    page.wait_for_url(KEYCLOAK + "/realms/sisplan/**")
    page.locator('[name="username"]').fill("f0-multi")
    page.locator('[name="password"]').fill(password)
    page.locator('#kc-form-login [type="submit"]').click()
    # El proveedor puede exigir un paso TOTP nuevo si esta cuenta acaba de autenticarse.
    with page.expect_response(sesion, timeout=70_000) as inicio:
        completar_otp(page, semilla_cuenta(semilla, "multi"))
    comprobar_rol(inicio.value, A, "AnalistaPlanificacion")
    return inicio.value.json()


def modo_autonomo(page: Page, modo: str, data, directory: Path | None):
    pei = modo == "solo-pei"
    modulos = next(i["modulos"] for i in data["instituciones"] if i["id"] == A)
    assert modulos == {"pei": pei, "poa": not pei}
    permitido, deshabilitado = ("peis", "poas") if pei else ("poas", "peis")
    titulo = "Planes estratégicos institucionales" if pei else "Planes operativos anuales"
    permitido_nav = "Planes estratégicos" if pei else "Planes operativos"
    denegado_nav = "Planes operativos" if pei else "Planes estratégicos"

    consultas_indebidas = []

    def observar(solicitud):
        ruta = urlsplit(solicitud.url).path
        if ruta == f"/api/v1/{deshabilitado}" or ruta.startswith(f"/api/v1/{deshabilitado}/"):
            consultas_indebidas.append(ruta)

    page.on("request", observar)
    # La URL directa representa una entrada por marcador con sesión activa.
    page.goto(f"{WEB}/{deshabilitado}")
    expect(page).to_have_url(f"{WEB}/{permitido}", timeout=15_000)
    expect(page.get_by_role("heading", name=titulo)).to_be_visible()
    expect(page.get_by_role("link", name=permitido_nav, exact=True)).to_be_visible()
    expect(page.get_by_role("link", name=denegado_nav, exact=True)).to_have_count(0)
    if pei:
        expect(page.locator(".count-label")).to_be_visible()
    else:
        page.get_by_role("button", name="Crear POA", exact=True).click()
        expect(page.get_by_label("Año del POA")).to_be_visible()
        expect(page.get_by_label("Plan estratégico", exact=True)).to_have_count(0)
    assert not consultas_indebidas, consultas_indebidas
    captura(page, directory, "03-solo-pei.png" if pei else "04-solo-poa.png")
    print(f"Angular: acceso directo y navegación autónoma verificados en {modo}.")


def verificar_salto_en_poa(page: Page, directory: Path | None):
    # Un documento nuevo reproduce la resolución de enlaces con base href.
    page.goto(WEB + "/poas")
    expect(page).to_have_url(WEB + "/poas", timeout=15_000)
    expect(page.get_by_role("heading", name="Planes operativos anuales", exact=True)).to_be_visible()
    documento = page.evaluate("performance.timeOrigin")
    page.keyboard.press("Tab")
    expect(page.get_by_role("link", name="Saltar al contenido", exact=True)).to_be_focused()
    page.keyboard.press("Enter")
    expect(page.locator("#main-content")).to_be_focused()
    expect(page).to_have_url(WEB + "/poas")
    assert page.evaluate("performance.timeOrigin") == documento, "El salto recargó el documento."
    captura(page, directory, "F0-QA-08-salto-en-poa.png")
    print("QA-08: Tab y Enter conservan /poas y enfocan el contenido sin recargar.", flush=True)


def crear_poa_de_area(page: Page, directory: Path | None):
    page.get_by_role("button", name="Crear POA", exact=True).click()
    dependencia = page.get_by_role("combobox", name="Dependencia", exact=True)
    area = "00000000-0000-4000-8000-000000000102"
    expect(dependencia.locator("option", has_text="Tecnología")).to_have_count(1)
    expect(dependencia.locator("option", has_text="Archivo anterior")).to_have_count(0)
    expect(dependencia.locator("option", has_text="Operaciones")).to_have_count(0)
    dependencia.select_option(area)
    anio = page.get_by_label("Año del POA", exact=True)
    monto = page.get_by_label("Presupuesto de referencia (RD$, opcional)", exact=True)
    guardar = page.get_by_role("button", name="Guardar POA", exact=True)
    envios = []

    def registrar(solicitud):
        if solicitud.url == WEB + "/api/v1/poas" and solicitud.method == "POST":
            envios.append(solicitud.post_data_json)

    page.on("request", registrar)
    viewport = page.viewport_size
    page.set_viewport_size({"width": 320, "height": 900})
    monto.fill("12,50")
    anio.fill("2243.5")
    expect(guardar).to_be_disabled()
    anio.press("Enter")
    aviso = page.locator("#poa-year-error")
    expect(aviso).to_contain_text("entero")
    expect(anio).to_have_attribute("aria-invalid", "true")
    expect(anio).to_have_attribute("aria-describedby", "poa-year-error")
    assert not envios, "El POA con año decimal llegó a la API."
    expect(monto).to_have_value("12,50")
    expect(dependencia).to_have_value(area)
    assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth + 1")
    captura(page, directory, "F0-QA-01-anio-invalido-320.png")

    anio.fill("2243")
    expect(aviso).to_have_count(0)
    expect(monto).to_have_value("12,50")
    expect(dependencia).to_have_value(area)
    with page.expect_response(lambda r: r.url == WEB + "/api/v1/poas" and r.request.method == "POST") as alta:
        guardar.click()
    assert alta.value.status == 201
    registro = alta.value.json()
    assert (registro["anio"], registro["presupuestoReferencia"], registro["dependenciaId"]) == (2243, "12.50", area)
    assert [envio["anio"] for envio in envios] == [2243]
    page.remove_listener("request", registrar)
    if viewport:
        page.set_viewport_size(viewport)
    expect(page.get_by_text("POA 2243 creado.", exact=True)).to_be_visible()
    fila = page.get_by_role("row").filter(has=page.get_by_role("cell", name="2243", exact=True))
    expect(fila.get_by_role("cell", name="Tecnología", exact=True)).to_be_visible()
    print("QA-01: el año decimal muestra un aviso, no se envía y permite guardar al corregirlo.", flush=True)


def verificar(page: Page, password: str, semilla: str, directory: Path | None, modo: str):
    data = entrar_como_multi(page, password, semilla)
    if modo != "ambos":
        modo_autonomo(page, modo, data, directory)
        return
    entrar = page.get_by_role("button", name="Iniciar sesión", exact=True)
    selector = page.get_by_role("combobox", name="Institución activa")
    auditoria = page.get_by_role("link", name="Auditoría", exact=True)
    crear = page.get_by_role("link", name="Crear nuevo PEI").first
    expect(selector).to_have_value(A)
    verificar_salto_en_poa(page, directory)
    crear_poa_de_area(page, directory)
    page.get_by_role("link", name="Planes estratégicos", exact=True).click()
    expect(page.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
    expect(crear).to_be_visible()
    expect(auditoria).to_have_count(0)
    expect(page.locator(".count-label")).to_be_visible()
    captura(page, directory, "01-horizonte-analista.png")
    print("Angular: el analista de Horizonte puede formular un PEI.", flush=True)

    cambiar_institucion(page, B, "Auditor")
    with etapa_chromium("consultar el listado de PEI como auditor"):
        expect(page.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
        expect(crear).to_have_count(0)
    with etapa_chromium("verificar el listado de POA del auditor"):
        with page.expect_response(lambda r: r.url == WEB + "/api/v1/poas") as lista:
            page.get_by_role("link", name="Planes operativos", exact=True).click()
        assert lista.value.ok, "El Auditor no pudo consultar los POA de Bahía."
        expect(page.get_by_text("Consultando POA…", exact=True)).to_have_count(0)
        expect(page.get_by_role("heading", name="Planes operativos anuales")).to_be_visible()
        expect(page.get_by_role("button", name="Crear POA")).to_have_count(0)
        expect(page.get_by_role("cell", name="2243", exact=True)).to_have_count(0)

    with etapa_chromium("abrir el historial y comprobar el alcance de su solicitud"):
        with page.expect_response(lambda r: r.url == WEB + "/api/v1/auditoria") as consulta:
            auditoria.click()
        response = consulta.value
        assert response.ok, "El Auditor no pudo consultar su historial."
        assert response.request.headers.get("x-institucion-id") == B

    with etapa_chromium("verificar la tabla de auditoría de Bahía"):
        expect(page.get_by_role("heading", name="Historial de auditoría", exact=True)).to_be_visible()
        tabla = page.get_by_role("region", name="Cambios registrados", exact=True)
        expect(tabla.locator("tbody tr").first).to_be_visible()
        # La batería HTTP registra cambios con el analista de Bahía.
        expect(tabla.get_by_text("30000000-0000-4000-8000-000000000010").first).to_be_visible()
        expect(tabla.get_by_text("30000000-0000-4000-8000-000000000004")).to_have_count(0)
        captura(page, directory, "02-bahia-auditor.png")
        print("Angular: la misma cuenta en Bahía sólo consulta planes y su auditoría.", flush=True)

    with etapa_chromium("cerrar sesión y volver al ingreso"):
        page.get_by_role("button", name="Cerrar sesión", exact=True).click()
        expect(entrar).to_be_visible()
        # El cliente también oculta las pantallas al salir. Recargar comprueba la salida en Keycloak.
        page.wait_for_url(lambda url: urlsplit(url).netloc == urlsplit(WEB).netloc
                          and urlsplit(url).path in ("/", "/institucion", "/peis"))
        page.reload()
        expect(entrar).to_be_visible()
        expect(selector).to_have_count(0)
        entrar.click()
        page.wait_for_url(KEYCLOAK + "/realms/sisplan/**")
        expect(page.locator('[name="password"]')).to_be_visible()
        print("Angular: salir termina la sesión y el proveedor vuelve a pedir credenciales.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capturas", type=Path, help="Directorio para las capturas del modo verificado.")
    parser.add_argument("--modo", choices=("ambos", "solo-pei", "solo-poa"), default="ambos")
    args = parser.parse_args()
    password = os.environ.get("SISPLAN_F0_PASSWORD")
    semilla = os.environ.get("SISPLAN_F0_TOTP_SEED")
    if not password or not semilla:
        parser.error("Define SISPLAN_F0_PASSWORD y SISPLAN_F0_TOTP_SEED para las cuentas ficticias.")
    try:
        with sync_playwright() as playwright:
            with playwright.chromium.launch() as browser:
                with browser.new_context(viewport={"width": 1440, "height": 900}, locale="es-DO") as context:
                    verificar(context.new_page(), password, semilla, args.capturas, args.modo)
    except Error as exc:
        # Los errores de navegación de Keycloak pueden incluir un código OIDC en la URL.
        raise SystemExit(f"El recorrido Angular se detuvo ({type(exc).__name__}).") from None


if __name__ == "__main__":
    main()
