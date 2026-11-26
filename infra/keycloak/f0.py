"""Cuentas ficticias y recorrido OIDC de F0 en Keycloak local."""
import argparse
import base64
import hashlib
import hmac
import json
import os
import secrets
import struct
import time
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import parse_qs, urlencode, urlsplit
from urllib.request import Request, urlopen

ORIGEN = "http://127.0.0.1:8080"
OIDC = ORIGEN + "/realms/sisplan/protocol/openid-connect"
ADMIN = ORIGEN + "/admin/realms/sisplan"
RETORNO = "http://127.0.0.1:4200/peis"
CLIENTE = "sisplan-web"
CUENTAS = [
    ("super", "SuperAdministrador"), ("admin", "AdministradorInstitucional"),
    ("director-plan", "DirectorPlanificacion"), ("analista", "AnalistaPlanificacion"),
    ("responsable", "ResponsableArea"), ("director-area", "DirectorArea"),
    ("auditor", "Auditor"), ("autoridad", "MaximaAutoridad"), ("comite", "ComiteEstrategico"),
    ("analista-b", "AnalistaPlanificacion"), ("responsable-b", "ResponsableArea"),
    ("responsable-a2", "ResponsableArea"), ("responsable-inactivo", "ResponsableArea"),
    ("multi", "AnalistaPlanificacion"), ("sin-membresia", "AnalistaPlanificacion"),
]


def sub(numero):
    return f"30000000-0000-4000-8000-{numero:012d}"


def consultar(url, *, metodo=None, datos=None, formulario=False, token=None):
    cabecera = {"Accept": "application/json"}
    contenido = None
    if datos is not None:
        cabecera["Content-Type"] = "application/x-www-form-urlencoded" if formulario else "application/json"
        contenido = (urlencode(datos) if formulario else json.dumps(datos)).encode()
    if token:
        cabecera["Authorization"] = "Bearer " + token
    try:
        with urlopen(Request(url, contenido, cabecera, method=metodo), timeout=25) as respuesta:
            cuerpo = respuesta.read()
            return json.loads(cuerpo) if cuerpo else None
    except HTTPError as error:
        # Nunca emitir cabeceras, cuerpos, contraseñas, códigos OIDC ni tokens.
        raise SystemExit(f"Keycloak devolvió HTTP {error.code} en {urlsplit(url).path}.") from None


def token_admin():
    datos = {"grant_type": "password", "client_id": "admin-cli",
             "username": os.environ.get("SISPLAN_KEYCLOAK_ADMIN", "admin"),
             "password": os.environ["SISPLAN_KEYCLOAK_PASSWORD"]}
    return consultar(ORIGEN + "/realms/master/protocol/openid-connect/token",
                     datos=datos, formulario=True)["access_token"]


def cliente_web():
    realm = json.loads(Path(__file__).with_name("sisplan-realm.json").read_text())
    return next(c for c in realm["clients"] if c["clientId"] == CLIENTE)


def preparar_retorno_local(admin):
    # Keycloak conserva los realms existentes al arrancar con --import-realm.
    ficha, = consultar(ADMIN + "/clients?" + urlencode({"clientId": CLIENTE}), token=admin)
    ruta = ADMIN + "/clients/" + ficha["id"]
    cliente = consultar(ruta, token=admin)
    atributo = "post.logout.redirect.uris"
    atributos = cliente.setdefault("attributes", {})
    anteriores = atributos.get(atributo, "").split("##")
    requeridas = cliente_web()["attributes"][atributo].split("##")
    faltantes = [uri for uri in requeridas if uri not in anteriores]
    if faltantes:
        atributos[atributo] = "##".join([uri for uri in anteriores if uri] + faltantes)
        consultar(ruta, metodo="PUT", datos=cliente, token=admin)


def alta_usuarios(password, semilla):
    usuarios = []
    for numero, (nombre, rol) in enumerate(CUENTAS, 1):
        credenciales = [{"type": "password", "value": password, "temporary": False}]
        if nombre == "autoridad" and semilla:
            credenciales.append({
                "type": "otp", "userLabel": "Segundo factor local F0",
                "credentialData": json.dumps({"subType": "totp", "digits": 6, "period": 30,
                                               "counter": 0, "algorithm": "HmacSHA1"}),
                "secretData": json.dumps({"value": semilla}),
            })
        usuarios.append({"id": sub(numero), "username": "f0-" + nombre, "enabled": True,
                         "email": "f0-" + nombre + "@example.invalid", "emailVerified": True,
                         "firstName": "Prueba", "lastName": nombre, "credentials": credenciales,
                         "clientRoles": {CLIENTE: [rol]}})
    admin = token_admin()
    preparar_retorno_local(admin)
    resultado = consultar(ADMIN + "/partialImport", token=admin,
                          datos={"ifResourceExists": "SKIP", "users": usuarios})
    existentes = consultar(ADMIN + "/users?search=f0-&max=200", token=admin)
    por_nombre = {u["username"]: u["id"] for u in existentes}
    for usuario in usuarios:
        if por_nombre.get(usuario["username"]) != usuario["id"]:
            raise SystemExit("El sub de la cuenta de prueba no coincide: " + usuario["username"])
    print(f"15 identidades verificadas. Altas: {resultado['added']}. Existentes: {resultado['skipped']}.")


def b64(dato):
    return base64.urlsafe_b64encode(dato).decode().rstrip("=")


def totp(semilla):
    paso = struct.pack(">Q", int(time.time()) // 30)
    firma = hmac.digest(semilla.encode(), paso, "sha1")
    indice = firma[-1] & 15
    valor = struct.unpack(">I", firma[indice:indice + 4])[0] & 0x7fffffff
    return f"{valor % 1000000:06d}"


def login(navegador, cliente, nombre, password, semilla, *, probar_salida=False):
    verificador = secrets.token_urlsafe(48)
    estado = secrets.token_urlsafe(24)
    parametros = {"client_id": cliente, "redirect_uri": RETORNO, "response_type": "code",
                  "scope": "openid", "state": estado, "code_challenge_method": "S256",
                  "code_challenge": b64(hashlib.sha256(verificador.encode()).digest())}
    with navegador.new_context() as contexto:
        pagina = contexto.new_page()
        pagina.route(RETORNO + "**", lambda ruta: ruta.fulfill(status=200, body="SISPLAN F0"))
        pagina.goto(OIDC + "/auth?" + urlencode(parametros))
        pagina.locator('[name="username"]').fill("f0-" + nombre)
        pagina.locator('[name="password"]').fill(password)
        pagina.locator('#kc-form-login [type="submit"]').click()
        if semilla:
            pagina.locator('[name="otp"]').fill(totp(semilla))
            pagina.locator('#kc-otp-login-form [type="submit"]').click()
        pagina.wait_for_url(RETORNO + "?**")
        callback = parse_qs(urlsplit(pagina.url).query)
        if callback.get("state") != [estado] or "code" not in callback:
            raise SystemExit("El retorno OIDC no coincide con el inicio de sesión.")
        tokens = consultar(OIDC + "/token", formulario=True, datos={
            "grant_type": "authorization_code", "code": callback["code"][0],
            "code_verifier": verificador, "redirect_uri": RETORNO, "client_id": cliente})
        if probar_salida:
            pagina.goto(OIDC + "/logout?" + urlencode({"id_token_hint": tokens["id_token"],
                                                        "post_logout_redirect_uri": RETORNO}))
            pagina.wait_for_url(RETORNO)
            pagina.goto(OIDC + "/auth?" + urlencode({**parametros, "prompt": "none"}))
            pagina.wait_for_url(RETORNO + "?**")
            if parse_qs(urlsplit(pagina.url).query).get("error") != ["login_required"]:
                raise SystemExit("La sesión del navegador continuó activa después de salir.")
        return tokens["access_token"]


def declaraciones(token):
    cuerpo = token.split(".")[1]
    return json.loads(base64.urlsafe_b64decode(cuerpo + "=" * (-len(cuerpo) % 4)))


def emitir_tokens(destino, password, semilla):
    from playwright.sync_api import Error, sync_playwright

    cliente_corto = "sisplan-f0-caducidad-" + secrets.token_hex(8)
    cliente = cliente_web()
    cliente["clientId"] = cliente_corto
    cliente["attributes"]["access.token.lifespan"] = "1"
    admin = token_admin()
    consultar(ADMIN + "/clients", datos=cliente, token=admin)
    uuid = consultar(ADMIN + "/clients?" + urlencode({"clientId": cliente_corto}), token=admin)[0]["id"]
    nombre = "analista"
    try:
        with sync_playwright() as playwright:
            with playwright.chromium.launch() as navegador:
                expirado = login(navegador, cliente_corto, "analista", password, None, probar_salida=True)
                claims = declaraciones(expirado)
                if not 1 <= claims["exp"] - claims["iat"] <= 2:
                    raise SystemExit("El cliente de prueba no emitió un token de un segundo.")
                tokens = {"expirado": expirado}
                for numero, (nombre, _) in enumerate(CUENTAS, 1):
                    token = login(navegador, CLIENTE, nombre, password,
                                  semilla if nombre == "autoridad" else None)
                    if declaraciones(token)["sub"] != sub(numero):
                        raise SystemExit("Identidad OIDC inesperada para f0-" + nombre)
                    clave = "sinMembresia" if nombre == "sin-membresia" else f"{numero:02d}"
                    tokens[clave] = token
    except Error as error:
        # Playwright puede incluir una URL con un código de un solo uso en su traza.
        raise SystemExit(f"Falló el recorrido del navegador para f0-{nombre} ({type(error).__name__}).") from None
    finally:
        consultar(ADMIN + "/clients/" + uuid, metodo="DELETE", token=token_admin())
    # La API admite hasta 30 s de diferencia entre relojes.
    time.sleep(max(0, declaraciones(expirado)["exp"] + 32 - time.time()))
    with os.fdopen(os.open(destino, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600), "w") as archivo:
        json.dump(tokens, archivo)
    print("PKCE, cierre de sesión y sujetos verificados." +
          (" Desafío TOTP de Máxima Autoridad verificado." if semilla else ""))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    acciones = parser.add_subparsers(dest="accion", required=True)
    acciones.add_parser("usuarios")
    tokens = acciones.add_parser("tokens")
    tokens.add_argument("--salida", type=Path, required=True, help="Archivo temporal nuevo (modo 600)")
    args = parser.parse_args()
    password = os.environ.get("SISPLAN_F0_PASSWORD")
    if not password or not os.environ.get("SISPLAN_KEYCLOAK_PASSWORD"):
        parser.error("Define SISPLAN_KEYCLOAK_PASSWORD y SISPLAN_F0_PASSWORD.")
    semilla = os.environ.get("SISPLAN_F0_TOTP_SEED")
    if semilla and (len(semilla) < 32 or not semilla.isascii()):
        parser.error("SISPLAN_F0_TOTP_SEED requiere al menos 32 caracteres ASCII aleatorios.")
    if args.accion == "usuarios":
        alta_usuarios(password, semilla)
    else:
        if args.salida.exists():
            parser.error("--salida requiere una ruta nueva.")
        emitir_tokens(args.salida, password, semilla)


if __name__ == "__main__":
    main()
