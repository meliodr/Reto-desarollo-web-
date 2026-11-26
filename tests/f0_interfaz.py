"""Recorrido en Angular real de una cuenta con roles distintos en dos instituciones."""

import argparse
import os
from pathlib import Path
from urllib.parse import urlsplit

from playwright.sync_api import Error, Page, Response, expect, sync_playwright

WEB = "http://127.0.0.1:4200"
KEYCLOAK = "http://127.0.0.1:8080"
A = "00000000-0000-4000-8000-000000000001"
B = "00000000-0000-4000-8000-000000000002"


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


def verificar(page: Page, password: str, directory: Path | None):
    page.goto(WEB + "/institucion")
    entrar = page.get_by_role("button", name="Iniciar sesión", exact=True)
    selector = page.get_by_role("combobox", name="Institución activa")
    auditoria = page.get_by_role("link", name="Auditoría", exact=True)
    crear = page.get_by_role("link", name="Crear nuevo PEI").first

    entrar.click()
    page.wait_for_url(KEYCLOAK + "/realms/sisplan/**")
    page.locator('[name="username"]').fill("f0-multi")
    page.locator('[name="password"]').fill(password)
    with page.expect_response(sesion) as inicio:
        page.locator('#kc-form-login [type="submit"]').click()
    comprobar_rol(inicio.value, A, "AnalistaPlanificacion")
    expect(selector).to_have_value(A)
    page.get_by_role("link", name="Planes estratégicos", exact=True).click()
    expect(page.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
    expect(crear).to_be_visible()
    expect(auditoria).to_have_count(0)
    expect(page.locator(".count-label")).to_be_visible()
    captura(page, directory, "01-horizonte-analista.png")
    print("Angular: el analista de Horizonte puede formular un PEI.")

    with page.expect_response(sesion) as cambio:
        selector.select_option(B)
    comprobar_rol(cambio.value, B, "Auditor")
    expect(selector).to_have_value(B)
    expect(page.get_by_role("heading", name="Planes estratégicos institucionales")).to_be_visible()
    expect(crear).to_have_count(0)
    with page.expect_response(lambda r: r.url == WEB + "/api/v1/poas") as lista:
        page.get_by_role("link", name="Planes operativos", exact=True).click()
    assert lista.value.ok, "El Auditor no pudo consultar los POA de Bahía."
    expect(page.get_by_text("Consultando POA…", exact=True)).to_have_count(0)
    expect(page.get_by_role("heading", name="Planes operativos anuales")).to_be_visible()
    expect(page.get_by_role("button", name="Crear POA")).to_have_count(0)

    with page.expect_response(lambda r: r.url == WEB + "/api/v1/auditoria") as consulta:
        auditoria.click()
    response = consulta.value
    assert response.ok, "El Auditor no pudo consultar su historial."
    rows = response.json()
    assert response.request.headers.get("x-institucion-id") == B
    assert all(row["institucionId"] == B for row in rows)
    expect(page.get_by_role("heading", name="Historial de auditoría")).to_be_visible()
    if rows:
        expect(page.locator("tbody tr")).to_have_count(len(rows))
    else:
        expect(page.get_by_text("Aún no se han registrado cambios en esta institución.")).to_be_visible()
    captura(page, directory, "02-bahia-auditor.png")
    print("Angular: la misma cuenta en Bahía sólo consulta planes y su auditoría.")

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
    parser.add_argument("--capturas", type=Path, help="Directorio para dos capturas del sistema.")
    args = parser.parse_args()
    password = os.environ.get("SISPLAN_F0_PASSWORD")
    if not password:
        parser.error("Define SISPLAN_F0_PASSWORD para las cuentas ficticias del realm local.")
    try:
        with sync_playwright() as playwright:
            with playwright.chromium.launch() as browser:
                with browser.new_context(viewport={"width": 1440, "height": 900}, locale="es-DO") as context:
                    verificar(context.new_page(), password, args.capturas)
    except Error as exc:
        # Los errores de navegación de Keycloak pueden incluir un código OIDC en la URL.
        raise SystemExit(f"El recorrido Angular se detuvo ({type(exc).__name__}).") from None


if __name__ == "__main__":
    main()
