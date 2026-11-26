-- Base de prueba recién migrada a 0007; preparar-historicos.sql debe ejecutarse primero.
-- Un PEI y sus dependientes pertenecen a A; un POA autónomo del mismo año pertenece a B.
INSERT INTO sisplan.pei(id) VALUES ('40000000-0000-4000-8000-000000000001');
INSERT INTO sisplan.pei_version(id, pei_id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores)
VALUES ('40000000-0000-4000-8000-000000000002', '40000000-0000-4000-8000-000000000001',
        'PEI anterior a F0', 'ordinario', '2030-01-01', '2033-12-31', 'Servir', 'Mejorar', 'Respeto');
INSERT INTO sisplan.eje (id, pei_version_id) VALUES
    ('40000000-0000-4000-8000-000000000003', '40000000-0000-4000-8000-000000000002');
INSERT INTO sisplan.eje_version(eje_id, revision, nombre, activo) VALUES
    ('40000000-0000-4000-8000-000000000003', 1, 'Primer eje', true);
INSERT INTO sisplan.objetivo(id, eje_id, eje_revision, codigo, nombre, descripcion, resultado_esperado, linea_accion) VALUES
    ('40000000-0000-4000-8000-000000000004', '40000000-0000-4000-8000-000000000003', 1,
     'OE1', 'Reducir las esperas', 'Mejorar los trámites', 'Menos esperas', 'Digitalizar solicitudes');
INSERT INTO sisplan.indicador(id, objetivo_id, nombre) VALUES
    ('40000000-0000-4000-8000-000000000005', '40000000-0000-4000-8000-000000000004', 'Tiempo de espera');
INSERT INTO sisplan.indicador_medicion(id, indicador_id, periodo, valor) VALUES
    ('40000000-0000-4000-8000-000000000006', '40000000-0000-4000-8000-000000000005', '2030-10-01', 17.25);
INSERT INTO sisplan.poa(id, pei_id, pei_version_id, anio, fecha_inicio, fecha_fin, presupuesto_referencia)
VALUES ('40000000-0000-4000-8000-000000000007', '40000000-0000-4000-8000-000000000001',
        '40000000-0000-4000-8000-000000000002', 2030, '2030-01-01', '2030-12-31', NULL),
       ('40000000-0000-4000-8000-000000000008', NULL, NULL, 2030, '2030-01-01', '2030-12-31', 1000.25);
INSERT INTO sisplan.poa_evaluacion(id, poa_id, cuatrimestre, fecha_inicio, fecha_fin, estado)
VALUES ('40000000-0000-4000-8000-000000000009', '40000000-0000-4000-8000-000000000007',
        1, '2030-01-01', '2030-04-30', 'generada');

INSERT INTO sisplan.f0_destino_historico(id, nombre) VALUES
    ('00000000-0000-4000-8000-000000000001', 'Institución A de prueba'),
    ('00000000-0000-4000-8000-000000000002', 'Institución B de prueba');
INSERT INTO sisplan.f0_mapa_historico(entidad, registro_id, institucion_id) VALUES
    ('pei', '40000000-0000-4000-8000-000000000001', '00000000-0000-4000-8000-000000000001'),
    ('poa', '40000000-0000-4000-8000-000000000008', '00000000-0000-4000-8000-000000000002');
