"""Valida OpenAPI 3.1, los esquemas de F0 y sus ejemplos."""

import argparse
import json
from copy import deepcopy
from pathlib import Path

import yaml
from jsonschema import Draft202012Validator, FormatChecker, RefResolver

ROOT = Path(__file__).resolve().parent


def check_references(node, document):
    if isinstance(node, list):
        for item in node:
            check_references(item, document)
    if isinstance(node, dict):
        for key, value in node.items():
            if key == "$ref":
                if not value.startswith("#/"):
                    raise ValueError(f"Referencia no local: {value}")
                target = document
                for part in value[2:].split("/"):
                    target = target[part.replace("~1", "/").replace("~0", "~")]
            else:
                check_references(value, document)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--esquema-oas", type=Path, help="Esquema oficial OpenAPI 3.1 en YAML o JSON")
    options = parser.parse_args()
    api = yaml.safe_load((ROOT / "pei.openapi.yaml").read_text())
    schema = json.loads((ROOT / "evidencia.schema.json").read_text())
    data = json.loads((ROOT / "fixtures/f0.json").read_text())
    formats = FormatChecker()

    check_references(api, api)
    if options.esquema_oas:
        official = yaml.safe_load(options.esquema_oas.read_text())
        Draft202012Validator(official, format_checker=formats).validate(api)
    else:
        from openapi_spec_validator import OpenAPIV31SpecValidator

        OpenAPIV31SpecValidator(api).validate()

    for component in api["components"]["schemas"].values():
        Draft202012Validator.check_schema(component)
    check_references(schema, schema)
    Draft202012Validator.check_schema(schema)
    evidence = Draft202012Validator(schema, format_checker=formats)
    evidence.validate(data["evidencia"])
    # Un reemplazo conserva el tipo de entidad del módulo y aplica límites de archivo.
    invalid = deepcopy(data["evidencia"])
    invalid["vinculo"]["modulo"] = "pei"
    if evidence.is_valid(invalid):
        raise ValueError("Un avance no es una entidad PEI.")
    invalid["vinculo"]["entidad"] = "medicion"
    evidence.validate(invalid)
    invalid["versiones"][-1]["fuente"]["bytes"] = 10485761
    if evidence.is_valid(invalid):
        raise ValueError("El contrato debe rechazar archivos mayores de 10 MiB.")

    paths = api["paths"]
    fixtures = [
        ("sesion", paths["/sesion"]["get"]["responses"]["200"]),
        ("organizacion", paths["/organizacion"]["get"]["responses"]["200"]),
        ("auditoria", paths["/auditoria"]["get"]["responses"]["200"]),
        ("crearPei", api["components"]["requestBodies"]["Pei"]),
        ("crearPoaAutonomo", paths["/poas"]["post"]["requestBody"]),
        ("poaAutonomo", paths["/poas"]["post"]["responses"]["201"]),
    ]
    resolver = RefResolver.from_schema(api)
    for key, body in fixtures:
        dto = body["content"]["application/json"]["schema"]
        Draft202012Validator(dto, resolver=resolver, format_checker=formats).validate(data[key])
    operations = sum(
        method in {"get", "put", "post", "delete", "options", "head", "patch", "trace"}
        for item in paths.values() for method in item
    )
    print(f"OpenAPI 3.1 válido: {len(paths)} rutas, {operations} operaciones y {len(data)} ejemplos de F0.")


if __name__ == "__main__":
    main()
