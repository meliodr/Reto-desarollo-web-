import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MapaEstrategico, Riesgo } from './pei-mapa-api';
import { PeiMapa } from './pei-mapa';

const mapaUrl = '/api/v1/peis/pei-1/mapa';
const objetivoUrl = '/api/v1/peis/pei-1/ejes/eje-1/objetivos/objetivo-1';
const riesgoUrl = `${objetivoUrl}/riesgos/riesgo-1`;
const riesgo: Riesgo = {
  id: 'riesgo-1',
  objetivoId: 'objetivo-1',
  nombre: 'Interrupción de servicios',
  descripcion: 'Los servicios pueden quedar interrumpidos.',
  probabilidad: 3,
  impacto: 5,
  tratamiento: null,
  accionMitigacion: null,
  responsable: null,
  fechaRevision: null,
  evidenciaUrl: null,
  cerrado: false,
  nivel: 'alto',
  puntuacion: 15,
  revision: 2,
};
const datos: MapaEstrategico = {
  peiId: 'pei-1',
  versionId: 'version-1',
  estado: 'borrador',
  ejes: [
    {
      id: 'eje-1',
      nombre: 'Atención cercana',
      descripcion: null,
      activo: true,
      revision: 1,
      objetivos: [
        {
          id: 'objetivo-1',
          codigo: 'OE-01',
          nombre: 'Menos espera',
          descripcion: 'Reducir la espera en los puntos de servicio.',
          resultadoEsperado: 'Atención oportuna',
          lineaAccion: 'Ampliar servicios digitales',
          activo: true,
          revision: 1,
          indicadores: [
            {
              id: 'indicador-1',
              nombre: 'Tiempo de espera',
              formula: null,
              unidad: null,
              lineaBase: null,
              meta: null,
              frecuencia: null,
              sentido: null,
              metodoCalculo: null,
              listoParaMedir: false,
              revision: 1,
            },
          ],
          riesgos: [riesgo],
        },
      ],
    },
  ],
};

function abrir(versionId?: string) {
  TestBed.configureTestingModule({
    imports: [PeiMapa],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(PeiMapa);
  fixture.componentRef.setInput('peiId', 'pei-1');
  if (versionId) fixture.componentRef.setInput('versionId', versionId);
  fixture.detectChanges();
  const http = TestBed.inject(HttpTestingController);
  http.expectNone(mapaUrl);
  fixture.componentInstance.alternar();
  fixture.detectChanges();
  http
    .expectOne({ method: 'GET', url: versionId ? `${mapaUrl}?versionId=${versionId}` : mapaUrl })
    .flush(datos);
  fixture.detectChanges();
  const vista: HTMLElement = fixture.nativeElement;
  return { fixture, component: fixture.componentInstance, http, vista };
}

it('consulta la versión indicada y filtra su jerarquía sin exponer acciones de edición', () => {
  const { fixture, component, http, vista } = abrir('version-1');
  component.texto.set('atencion oportuna');
  component.filtro.set('pendientes');
  fixture.detectChanges();
  expect(vista.querySelector('details')?.open).toBe(true);
  expect(vista.textContent).toContain('Pendiente de configuración');
  expect(vista.textContent).toContain('Riesgo alto');
  expect(vista.textContent).not.toContain('Editar riesgo');
  component.texto.set('documento ausente');
  fixture.detectChanges();
  expect(vista.textContent).toContain('No encontramos resultados');
  http.verify();
});

it('exige mitigación completa y envía If-Match al cerrar un riesgo alto', () => {
  const { component, http } = abrir();
  const [eje] = datos.ejes;
  component.prepararRiesgo(eje, eje.objetivos[0], riesgo);
  http.expectOne({ method: 'GET', url: riesgoUrl }).flush(riesgo, {
    headers: { ETag: '"riesgo-2"' },
  });
  component.form.controls.cerrado.setValue(true);
  component.guardarRiesgo();
  expect(component.errorCambios()).toContain('completa el tratamiento');
  http.expectNone({ method: 'PUT', url: riesgoUrl });
  const cambios = {
    tratamiento: 'Mitigar',
    accionMitigacion: 'Probar redundancia.',
    responsable: 'Tecnología',
    fechaRevision: '2027-02-03',
    evidenciaUrl: 'https://evidencia.example/acta',
  };
  component.form.patchValue(cambios);
  component.guardarRiesgo();
  const guardar = http.expectOne({ method: 'PUT', url: riesgoUrl });
  expect(guardar.request.headers.get('If-Match')).toBe('"riesgo-2"');
  expect(guardar.request.body).toMatchObject({ ...cambios, cerrado: true });
  guardar.flush({ ...riesgo, ...cambios, cerrado: true, revision: 3 });
  http.expectOne(mapaUrl).flush(datos);
  expect(component.edicion()).toBeNull();
  http.verify();
});

it('actualiza el estado del objetivo con su revisión y vuelve a cargar el mapa', () => {
  const { component, http } = abrir();
  const [eje] = datos.ejes;
  const objetivo = eje.objetivos[0];
  component.pendienteConfirmar.set({ eje, objetivo });
  component.cambiarEstado();
  http.expectOne({ method: 'GET', url: objetivoUrl }).flush(objetivo, {
    headers: { ETag: '"objetivo-1"' },
  });
  const cambio = http.expectOne({ method: 'PATCH', url: `${objetivoUrl}/estado` });
  expect(cambio.request.headers.get('If-Match')).toBe('"objetivo-1"');
  expect(cambio.request.body).toEqual({ activo: false });
  cambio.flush({ ...objetivo, activo: false, revision: 2 });
  http.expectOne(mapaUrl).flush(datos);
  expect(component.aviso()).toBe('Objetivo desactivado.');
  http.verify();
});

it('conserva la edición por riesgo cuando otra persona guarda primero', () => {
  const { component, http } = abrir();
  const [eje] = datos.ejes;
  component.prepararRiesgo(eje, eje.objetivos[0], riesgo);
  http.expectOne(riesgoUrl).flush(riesgo, { headers: { ETag: '"riesgo-2"' } });
  component.form.controls.descripcion.setValue('Mi valoración revisada.');
  component.guardarRiesgo();
  http
    .expectOne({ method: 'PUT', url: riesgoUrl })
    .flush(
      { title: 'La revisión del riesgo cambió.' },
      { status: 412, statusText: 'Precondition Failed' },
    );
  expect(component.form.controls.descripcion.value).toBe('Mi valoración revisada.');
  expect(component.incierto()).toBe(true);
  component.guardarRiesgo();
  http.expectNone({ method: 'PUT', url: riesgoUrl });
  component.cancelarRiesgo();
  http.expectOne(mapaUrl).flush(datos);
  expect(component.edicion()).toBeNull();
  http.verify();
});
