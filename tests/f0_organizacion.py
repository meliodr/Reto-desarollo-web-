"""Compara el catálogo de cada institución con los datos ficticios de F0."""


def uuid_prueba(numero):
    return f"00000000-0000-4000-8000-{numero:012d}"


def check_organizacion(api):
    comunes = {"unidad": "Unidad", "porcentaje": "Porcentaje"}
    casos = (
        ("04", 1, {101: ("Planificación", True), 102: ("Tecnología", True),
                   103: ("Archivo anterior", False)}, 1001, 2029, {"expediente": "Expediente"}),
        ("10", 2, {201: ("Planificación", True), 202: ("Operaciones", True)},
         2001, 2030, {"inspeccion": "Inspección"}),
    )
    for cuenta, institucion, areas, periodo_pei, fin_pei, unidades in casos:
        org, _ = api(cuenta, "/organizacion", 200, institution=uuid_prueba(institucion))
        assert {d["id"]: (d["nombre"], d["activa"]) for d in org["dependencias"]} == {
            uuid_prueba(i): info for i, info in areas.items()
        }
        assert {p["id"]: (p["nombre"], p["fechaInicio"], p["fechaFin"]) for p in org["periodos"]} == {
            uuid_prueba(periodo_pei): (f"PEI 2026–{fin_pei}", "2026-01-01", f"{fin_pei}-12-31"),
            uuid_prueba(periodo_pei + 1): ("POA 2026", "2026-01-01", "2026-12-31"),
        }
        assert {u["codigo"]: u["nombre"] for u in org["unidadesMedida"]} == comunes | unidades

        cuenta_area = "05" if institucion == 1 else "11"
        restringida, _ = api(cuenta_area, "/organizacion", 200, institution=uuid_prueba(institucion))
        propia = uuid_prueba(101 if institucion == 1 else 201)
        assert restringida["dependencias"] == [{"id": propia, "nombre": "Planificación", "activa": True}]
        assert restringida["periodos"] == org["periodos"]
        assert restringida["unidadesMedida"] == org["unidadesMedida"]
    print("F0-02: dependencias, períodos y unidades verificados en ambas instituciones y por área.")
