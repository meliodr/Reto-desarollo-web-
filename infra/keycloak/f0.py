"""Cuentas ficticias y recorrido OIDC de F0 en Keycloak local."""
import argparse
import base64
import hashlib
import hmac
import json
import os
import secrets
import struct
import sys
import time
from contextlib import contextmanager
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import parse_qs, urlencode, urlsplit
from urllib.request import Request, urlopen

ORIGEN = "http://127.0.0.1:8080"
OIDC = ORIGEN + "/realms/sisplan/protocol/openid-connect"
ADMIN = ORIGEN + "/admin/realms/sisplan"
RETORNO = "http://127.0.0.1:4200/f0-retorno.html"
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


def flujo_totp(admin):
    # El cliente de SISPLAN usa SSO o un formulario con sus dos pasos obligatorios.
    # Construir antes de enlazar permite reintentar si se interrumpe el aprovisionamiento.
    raiz = "sisplan-f0-mfa"
    formulario = "sisplan-f0-formulario"
    auth = ADMIN + "/authentication"
    flujos = auth + "/flows"
    existentes = {f["alias"]: f for f in consultar(flujos, token=admin)}
    if raiz not in existentes:
        consultar(flujos, datos={"alias": raiz, "providerId": "basic-flow", "topLevel": True}, token=admin)
        existentes = {f["alias"]: f for f in consultar(flujos, token=admin)}
    flujo_id = existentes[raiz]["id"]

    def pasos(alias):
        return [p for p in consultar(f"{flujos}/{alias}/executions", token=admin) if p["level"] == 0]

    def asegurar(alias, parent_id, proveedor, requisito, orden):
        paso = next((p for p in pasos(alias) if p.get("providerId") == proveedor), None)
        if paso is None:
            consultar(auth + "/executions", datos={"parentFlow": parent_id, "authenticator": proveedor,
                      "requirement": requisito, "priority": orden}, token=admin)
        elif (paso["requirement"], paso["priority"]) != (requisito, orden):
            raise SystemExit(f"La configuración local de {alias}/{proveedor} difiere de la política F0.")

    asegurar(raiz, flujo_id, "auth-cookie", "ALTERNATIVE", 10)
    ruta_raiz = f"{flujos}/{raiz}/executions"
    def subflujo():
        return next((p for p in pasos(raiz) if p.get("displayName") == formulario), None)
    forms = subflujo()
    if forms is None:
        consultar(ruta_raiz + "/flow", datos={"alias": formulario, "type": "basic-flow", "priority": 20}, token=admin)
        forms = subflujo()
    asegurar(formulario, forms["flowId"], "auth-username-password-form", "REQUIRED", 10)
    asegurar(formulario, forms["flowId"], "auth-otp-form", "REQUIRED", 20)
    if (len(pasos(raiz)), len(pasos(formulario))) != (2, 2):
        raise SystemExit("Revisa los pasos adicionales en el flujo de autenticación local de F0.")
    if forms["requirement"] != "ALTERNATIVE":
        consultar(ruta_raiz, metodo="PUT", datos={**forms, "requirement": "ALTERNATIVE"}, token=admin)
    return flujo_id


def preparar_cliente_local(admin):
    # Keycloak conserva los realms existentes al arrancar con --import-realm.
    ficha, = consultar(ADMIN + "/clients?" + urlencode({"clientId": CLIENTE}), token=admin)
    ruta = ADMIN + "/clients/" + ficha["id"]
    cliente = consultar(ruta, token=admin)
    atributo = "post.logout.redirect.uris"
    atributos = cliente.setdefault("attributes", {})
    anteriores = atributos.get(atributo, "").split("##")
    requeridas = cliente_web()["attributes"][atributo].split("##")
    faltantes = [uri for uri in requeridas if uri not in anteriores]
    bindings = cliente.setdefault("authenticationFlowBindingOverrides", {})
    flujo = flujo_totp(admin)
    if faltantes or bindings.get("browser") != flujo:
        bindings["browser"] = flujo
        atributos[atributo] = "##".join([uri for uri in anteriores if uri] + faltantes)
        consultar(ruta, metodo="PUT", datos=cliente, token=admin)


ETIQUETA_TOTP = "SISPLAN F0 · autenticador de demostración"
ETIQUETA_ANTERIOR = "Segundo factor local F0"


def semilla_cuenta(maestra, nombre):
    # Sólo la semilla derivada entra en la cuenta de Keycloak.
    return hmac.digest(maestra.encode(), ("sisplan-f0/totp/" + nombre).encode(), "sha256").hex()


def credencial_totp(maestra, nombre):
    return {
        "type": "otp", "userLabel": ETIQUETA_TOTP,
        "credentialData": json.dumps({"subType": "totp", "digits": 6, "period": 30,
                                      "counter": 0, "algorithm": "HmacSHA1"}),
        "secretData": json.dumps({"value": semilla_cuenta(maestra, nombre)}),
    }


def dispositivos(admin, sujeto):
    return [c for c in consultar(f"{ADMIN}/users/{sujeto}/credentials", token=admin) if c["type"] == "otp"]


def alta_usuarios(password, semilla):
    usuarios = []
    for numero, (nombre, rol) in enumerate(CUENTAS, 1):
        credenciales = [{"type": "password", "value": password, "temporary": False}, credencial_totp(semilla, nombre)]
        usuarios.append({"id": sub(numero), "username": "f0-" + nombre, "enabled": True,
                         "email": "f0-" + nombre + "@example.invalid", "emailVerified": True,
                         "firstName": "Prueba", "lastName": nombre, "credentials": credenciales,
                         "clientRoles": {CLIENTE: [rol]}})
    admin = token_admin()
    preparar_cliente_local(admin)
    resultado = consultar(ADMIN + "/partialImport", token=admin,
                          datos={"ifResourceExists": "SKIP", "users": usuarios})
    existentes = consultar(ADMIN + "/users?search=f0-&max=200", token=admin)
    por_nombre = {u["username"]: u["id"] for u in existentes}
    for usuario in usuarios:
        sujeto = usuario["id"]
        if por_nombre.get(usuario["username"]) != sujeto:
            raise SystemExit("El sub de la cuenta de prueba no coincide: " + usuario["username"])
        if not dispositivos(admin, sujeto):
            # Para un volumen anterior, añadir sólo la credencial ausente. Keycloak
            # exige validar también el perfil en el PUT; preservar sus campos.
            ruta = f"{ADMIN}/users/{sujeto}"
            perfil = consultar(ruta, token=admin)
            perfil["credentials"] = [usuario["credentials"][1]]
            consultar(ruta, metodo="PUT", datos=perfil, token=admin)
    print(f"15 identidades con TOTP. Altas: {resultado['added']}. Existentes: {resultado['skipped']}.")


def semilla_aprovisionada(admin, numero, nombre, maestra):
    credenciales = dispositivos(admin, sub(numero))
    if any(c.get("userLabel") == ETIQUETA_TOTP for c in credenciales):
        return semilla_cuenta(maestra, nombre)
    if nombre == "autoridad" and any(c.get("userLabel") == ETIQUETA_ANTERIOR for c in credenciales):
        anterior = os.environ.get("SISPLAN_F0_TOTP_AUTORIDAD_ANTERIOR")
        if anterior and anterior != maestra:
            return anterior
        raise SystemExit("Para esta Autoridad existente indica su semilla anterior en "
                         "SISPLAN_F0_TOTP_AUTORIDAD_ANTERIOR y una raíz F0 independiente.")
    raise SystemExit(f"f0-{nombre} no tiene un autenticador automatizable de F0. "
                     "Revisa el dispositivo registrado. Se conservan sus credenciales.")


def b64(dato):
    return base64.urlsafe_b64encode(dato).decode().rstrip("=")


def totp(semilla, instante=None):
    paso = struct.pack(">Q", int(time.time() if instante is None else instante) // 30)
    firma = hmac.digest(semilla.encode(), paso, "sha1")
    indice = firma[-1] & 15
    valor = struct.unpack(">I", firma[indice:indice + 4])[0] & 0x7fffffff
    return f"{valor % 1000000:06d}"


def enviar_formulario(pagina, selector):
    # También navega cuando Keycloak vuelve a representar el mismo formulario.
    with pagina.expect_navigation(wait_until="domcontentloaded"):
        pagina.locator(selector).click()


def completar_otp(pagina, semilla):
    campo = pagina.locator('[name="otp"]')
    campo.fill(totp(semilla))
    enviar_formulario(pagina, '#kc-otp-login-form [type="submit"]')
    # Esperar al nuevo documento evita confundir el formulario anterior con un rechazo.
    if campo.is_visible():
        time.sleep(31 - time.time() % 30)
        campo.fill(totp(semilla))
        enviar_formulario(pagina, '#kc-otp-login-form [type="submit"]')
        if campo.is_visible():
            raise SystemExit("Keycloak rechazó el TOTP. Revisa la semilla original del volumen de F0.")


@contextmanager
def paso_oidc(pagina, nombre, etapa, publicar):
    from playwright.sync_api import Error

    if publicar:
        print(f"f0-{nombre} · {etapa}", flush=True)
    try:
        yield
    except Error as error:
        # Sólo identificar controles públicos conocidos. Omitir URL, textos y valores.
        controles = {"usuario": '[name="username"]', "contraseña": '[name="password"]',
                     "TOTP": '[name="otp"]', "registro TOTP": '#kc-totp-settings-form',
                     "perfil": '#kc-update-profile-form', "confirmar salida": '#kc-logout-confirm',
                     "error de formulario": '[aria-invalid="true"], [role="alert"]',
                     "error de Keycloak": '#kc-error-message'}
        visibles = []
        try:
            for etiqueta, selector in controles.items():
                if pagina.locator(selector).first.is_visible():
                    visibles.append(etiqueta)
            ubicacion = {urlsplit(ORIGEN).netloc: "Keycloak", urlsplit(RETORNO).netloc: "retorno"}.get(
                urlsplit(pagina.url).netloc, "otra página")
        except Error:
            ubicacion = "página no disponible"
        estado = ", ".join(visibles) or "sin controles reconocidos"
        raise SystemExit(f"f0-{nombre} · {etapa} · {type(error).__name__} · {ubicacion} · {estado}") from None


class RetornoInvalido(SystemExit):
    pass


def login(navegador, cliente, nombre, password, semilla, *, probar_salida=False, probar_rechazo=False):
    verificador = secrets.token_urlsafe(48)
    estado = secrets.token_urlsafe(24)
    parametros = {"client_id": cliente, "redirect_uri": RETORNO, "response_type": "code",
                  "scope": "openid", "response_mode": "query", "state": estado, "code_challenge_method": "S256",
                  "code_challenge": b64(hashlib.sha256(verificador.encode()).digest())}
    with navegador.new_context() as contexto:
        pagina = contexto.new_page()
        if probar_salida:
            with paso_oidc(pagina, nombre, "página estática del recorrido F0", True):
                pagina.goto(RETORNO)
                pagina.locator("#retorno-f0").wait_for(timeout=5000)
        with paso_oidc(pagina, nombre, "formulario de contraseña", probar_salida):
            pagina.goto(OIDC + "/auth?" + urlencode(parametros))
            pagina.locator('[name="username"]').fill("f0-" + nombre)
            pagina.locator('[name="password"]').fill(password)
            enviar_formulario(pagina, '#kc-form-login [type="submit"]')
        if probar_rechazo:
            with paso_oidc(pagina, nombre, "rechazo de código incorrecto", True):
                # Tiene seis dígitos y difiere de los códigos válidos en la ventana.
                prohibidos = {totp(semilla, time.time() + d * 30) for d in range(-2, 3)}
                incorrecto = next(f"{n:06d}" for n in range(6) if f"{n:06d}" not in prohibidos)
                pagina.locator('[name="otp"]').fill(incorrecto)
                enviar_formulario(pagina, '#kc-otp-login-form [type="submit"]')
                pagina.locator('[name="otp"]').wait_for(state="visible")
                if urlsplit(pagina.url).netloc != urlsplit(ORIGEN).netloc:
                    raise SystemExit("Keycloak aceptó el código incorrecto.")
        with paso_oidc(pagina, nombre, "formulario TOTP", probar_salida):
            completar_otp(pagina, semilla)
        with paso_oidc(pagina, nombre, "retorno del código de autorización", probar_salida):
            pagina.wait_for_url(RETORNO + "?**")
        callback = parse_qs(urlsplit(pagina.url).query)
        if callback.get("state") != [estado] or "code" not in callback:
            errores = {"invalid_request", "unauthorized_client", "access_denied", "unsupported_response_type",
                       "invalid_scope", "server_error", "temporarily_unavailable", "interaction_required",
                       "login_required", "account_selection_required", "consent_required"}
            motivo = callback.get("error", ["ausente"])[0]
            clase = motivo if motivo in errores or motivo == "ausente" else "otro"
            coincide = callback.get("state") == [estado]
            tipo_cliente = "normal" if cliente == CLIENTE else "de caducidad"
            raise RetornoInvalido(f"f0-{nombre} · cliente {tipo_cliente} · retorno OIDC inválido · "
                                  f"state coincide={coincide} · hay código={'code' in callback} · error={clase}")
        tokens = consultar(OIDC + "/token", formulario=True, datos={
            "grant_type": "authorization_code", "code": callback["code"][0],
            "code_verifier": verificador, "redirect_uri": RETORNO, "client_id": cliente})
        if probar_salida:
            with paso_oidc(pagina, nombre, "cierre de sesión", True):
                pagina.goto(OIDC + "/logout?" + urlencode({"id_token_hint": tokens["id_token"],
                                                            "post_logout_redirect_uri": RETORNO}))
                pagina.wait_for_url(RETORNO)
            with paso_oidc(pagina, nombre, "verificación de salida sin interacción", True):
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
    cliente["attributes"]["post.logout.redirect.uris"] = RETORNO
    admin = token_admin()
    web, = consultar(ADMIN + "/clients?" + urlencode({"clientId": CLIENTE}), token=admin)
    configurado = consultar(ADMIN + "/clients/" + web["id"], token=admin)
    cliente["authenticationFlowBindingOverrides"] = configurado["authenticationFlowBindingOverrides"]
    semillas = {nombre: semilla_aprovisionada(admin, i, nombre, semilla)
                for i, (nombre, _) in enumerate(CUENTAS, 1)}
    consultar(ADMIN + "/clients", datos=cliente, token=admin)
    uuid = consultar(ADMIN + "/clients?" + urlencode({"clientId": cliente_corto}), token=admin)[0]["id"]
    nombre = "analista"
    try:
        with sync_playwright() as playwright:
            with playwright.chromium.launch() as navegador:
                expirado = login(navegador, cliente_corto, "analista", password, semillas["analista"], probar_salida=True)
                claims = declaraciones(expirado)
                if not 1 <= claims["exp"] - claims["iat"] <= 2:
                    raise SystemExit("El cliente de prueba no emitió un token de un segundo.")
                tokens = {"expirado": expirado}
                rechazos = []
                for numero, (nombre, _) in enumerate(CUENTAS, 1):
                    try:
                        token = login(navegador, CLIENTE, nombre, password,
                                      semillas[nombre], probar_rechazo=(nombre == "autoridad"))
                    except RetornoInvalido as rechazo:
                        print(rechazo, flush=True)
                        rechazos.append(nombre)
                        continue
                    if declaraciones(token)["sub"] != sub(numero):
                        raise SystemExit("Identidad OIDC inesperada para f0-" + nombre)
                    print(f"f0-{nombre} · contraseña, TOTP y sujeto verificados", flush=True)
                    clave = "sinMembresia" if nombre == "sin-membresia" else f"{numero:02d}"
                    tokens[clave] = token
                if rechazos:
                    raise SystemExit("Keycloak rechazó el retorno de las cuentas: " + ", ".join(rechazos))
    except Error as error:
        # Playwright puede incluir una URL con un código de un solo uso en su traza.
        raise SystemExit(f"Falló el recorrido del navegador para f0-{nombre} ({type(error).__name__}).") from None
    finally:
        consultar(ADMIN + "/clients/" + uuid, metodo="DELETE", token=token_admin())
    # La API admite hasta 30 s de diferencia entre relojes.
    time.sleep(max(0, declaraciones(expirado)["exp"] + 32 - time.time()))
    with os.fdopen(os.open(destino, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600), "w") as archivo:
        json.dump(tokens, archivo)
    print("F0-08: TOTP, PKCE y sujetos verificados para las 15 cuentas. Código erróneo rechazado y salida verificada.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    acciones = parser.add_subparsers(dest="accion", required=True)
    acciones.add_parser("usuarios")
    nombres = [nombre for nombre, _ in CUENTAS]
    codigo = acciones.add_parser("codigo", help="Código personal de una cuenta ficticia en terminal local")
    codigo.add_argument("cuenta", choices=nombres)
    codigo.add_argument("--registrar", action="store_true", help="Mostrar la clave personal para una app TOTP")
    tokens = acciones.add_parser("tokens")
    tokens.add_argument("--salida", type=Path, required=True, help="Archivo temporal nuevo (modo 600)")
    args = parser.parse_args()
    password = os.environ.get("SISPLAN_F0_PASSWORD")
    if not os.environ.get("SISPLAN_KEYCLOAK_PASSWORD"):
        parser.error("Define SISPLAN_KEYCLOAK_PASSWORD.")
    if args.accion != "codigo" and not password:
        parser.error("Define SISPLAN_F0_PASSWORD.")
    semilla = os.environ.get("SISPLAN_F0_TOTP_SEED")
    if not semilla or len(semilla) < 32 or not semilla.isascii():
        parser.error("SISPLAN_F0_TOTP_SEED requiere al menos 32 caracteres ASCII aleatorios.")
    if semilla == os.environ.get("SISPLAN_F0_TOTP_AUTORIDAD_ANTERIOR"):
        parser.error("La raíz de F0 debe ser independiente de la clave personal anterior de Autoridad.")
    if args.accion == "codigo":
        if not sys.stdout.isatty():
            parser.error("El código y la clave personal sólo se muestran en una terminal interactiva.")
        numero = nombres.index(args.cuenta) + 1
        personal = semilla_aprovisionada(token_admin(), numero, args.cuenta, semilla)
        print(base64.b32encode(personal.encode()).decode().rstrip("=") if args.registrar else totp(personal))
    elif args.accion == "usuarios":
        alta_usuarios(password, semilla)
    else:
        if args.salida.exists():
            parser.error("--salida requiere una ruta nueva.")
        emitir_tokens(args.salida, password, semilla)


if __name__ == "__main__":
    main()
