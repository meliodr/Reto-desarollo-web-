import { activateAnalyst } from '../../testing/membresia';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Pei } from './pei-api';
import { PeiForm } from './pei-form';

const pei: Pei = {
  id: 'pei-1',
  versionId: 'version-1',
  nombre: 'Plan institucional',
  tipo: 'ordinario',
  fechaInicio: '2026-01-01',
  fechaFin: '2029-12-31',
  mision: 'Servir a la comunidad.',
  vision: 'Una institución accesible.',
  valores: 'Transparencia y responsabilidad.',
  estado: 'borrador',
};
const workflowUrl = '/api/v1/pei-versiones/version-1/transiciones';

describe('Flujo del PEI en Angular', () => {
  let fixture: ComponentFixture<PeiForm>;
  let http: HttpTestingController;
  let view: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PeiForm],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ id: pei.id })) },
        },
      ],
    });
    activateAnalyst();
    fixture = TestBed.createComponent(PeiForm);
    http = TestBed.inject(HttpTestingController);
    view = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  function openPlan(estado: Pei['estado'], acciones: string[]) {
    http
      .expectOne('/api/v1/peis/pei-1')
      .flush({ ...pei, estado }, { headers: { ETag: '"rev-1"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1/ejes').flush([]);
    http.expectOne({ method: 'GET', url: workflowUrl }).flush({ acciones });
    fixture.detectChanges();
  }

  function button(label: string) {
    return [...view.querySelectorAll('button')].find((item) => item.textContent?.trim() === label);
  }

  function click(label: string) {
    const item = button(label);
    if (!item) throw new Error(`No se encontró el botón ${label}`);
    item.click();
    fixture.detectChanges();
  }

  it('protege los cambios locales y vuelve el plan de solo lectura al enviarlo', () => {
    openPlan('borrador', ['enviar']);
    const name = view.querySelector<HTMLInputElement>('#nombre')!;
    name.value = 'Edición sin guardar';
    name.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(button('Enviar a revisión')?.disabled).toBe(true);
    http.expectNone({ method: 'POST', url: workflowUrl });
    fixture.componentInstance.form.markAsPristine();
    fixture.detectChanges();
    click('Enviar a revisión');
    const submit = http.expectOne({ method: 'POST', url: workflowUrl });
    expect(submit.request.headers.get('If-Match')).toBe('"rev-1"');
    expect(submit.request.body.accion).toBe('enviar');
    submit.flush({ ...pei, estado: 'en_revision' }, { headers: { ETag: '"rev-2"' } });
    fixture.detectChanges();
    http.expectOne({ method: 'GET', url: workflowUrl }).flush({ acciones: ['devolver'] });
    fixture.detectChanges();
    expect(name.disabled).toBe(true);
    expect(button('Agregar eje')).toBeUndefined();
    expect(button('Aprobar PEI')).toBeUndefined();
    expect(view.textContent).toContain('En revisión');
  });

  it('carga la estructura de una nueva revisión al reemplazar la versión mostrada', () => {
    openPlan('borrador', ['enviar']);
    const form = fixture.componentInstance;
    form.fetchLatest();
    http
      .expectOne('/api/v1/peis/pei-1')
      .flush(
        { ...pei, versionId: 'version-2', versionOrigenId: pei.versionId },
        { headers: { ETag: '"rev-2"' } },
      );
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    try {
      form.replaceWithLatest();
      fixture.detectChanges();
      http.expectOne('/api/v1/peis/pei-1/ejes').flush([]);
      http.expectOne('/api/v1/pei-versiones/version-2/transiciones').flush({ acciones: [] });
    } finally {
      confirm.mockRestore();
    }
  });

  it('pide confirmar la publicación y muestra los POA y sus cuatrimestres', () => {
    openPlan('aprobado', ['publicar']);
    click('Publicar PEI');
    http.expectNone({ method: 'POST', url: workflowUrl });
    click('Confirmar publicación');
    const publication = http.expectOne({ method: 'POST', url: workflowUrl });
    expect(publication.request.body.accion).toBe('publicar');
    publication.flush({ ...pei, estado: 'publicado' }, { headers: { ETag: '"rev-2"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1/poas').flush([
      {
        id: 'poa-1',
        anio: 2026,
        estado: 'formulacion',
        vigente: true,
        fechaInicio: '2026-01-01',
        fechaFin: '2026-12-31',
        evaluaciones: [
          {
            id: 'cut-1',
            cuatrimestre: 1,
            estado: 'generada',
            fechaInicio: '2026-01-01',
            fechaFin: '2026-04-30',
          },
          {
            id: 'cut-2',
            cuatrimestre: 2,
            estado: 'generada',
            fechaInicio: '2026-05-01',
            fechaFin: '2026-08-31',
          },
          {
            id: 'cut-3',
            cuatrimestre: 3,
            estado: 'generada',
            fechaInicio: '2026-09-01',
            fechaFin: '2026-12-31',
          },
        ],
      },
    ]);
    fixture.detectChanges();
    expect(view.textContent).toContain('POA 2026');
    expect(view.textContent).toContain('En formulación');
    expect(view.querySelectorAll('.poa-detail li')).toHaveLength(3);
    expect(view.textContent).toContain('Cuatrimestre 3');
  });

  it('consulta la versión oficial y el motivo de devolución en modo solo PEI', () => {
    activateAnalyst(true, false);
    http
      .expectOne('/api/v1/peis/pei-1')
      .flush({ ...pei, estado: 'publicado' }, { headers: { ETag: '"rev-9"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1/ejes').flush([]);
    http.expectNone('/api/v1/peis/pei-1/poas');
    expect(view.querySelector('h1')?.textContent).toContain('PEI oficial');
    click('Ver versiones e historial');
    http
      .expectOne('/api/v1/peis/pei-1/versiones')
      .flush([{ versionId: 'version-1', numero: 1, revision: 9, estado: 'publicado' }]);
    http.expectOne('/api/v1/pei-versiones/version-1/historial').flush([
      {
        revision: 3,
        accion: 'devolver',
        estadoAnterior: 'en_revision',
        estadoNuevo: 'observado',
        motivo: 'Revisar el resultado esperado.',
        registradaEn: '2026-11-26T12:00:00Z',
        emisor: 'http://127.0.0.1:8080/realms/sisplan',
        sujeto: 'director-plan',
      },
    ]);
    fixture.detectChanges();
    expect(view.querySelector('a[href="/peis/pei-1/versiones/version-1"]')?.textContent).toContain(
      'Versión 1 · Publicado',
    );
    expect(view.textContent).toContain('Revisar el resultado esperado.');
    expect(view.textContent).toContain('director-plan');
  });

  it('abre una revisión de la versión oficial con motivo y protege su vigencia', () => {
    activateAnalyst(true, false);
    http
      .expectOne('/api/v1/peis/pei-1')
      .flush({ ...pei, estado: 'publicado' }, { headers: { ETag: '"rev-9"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1/ejes').flush([]);
    click('Ver versiones e historial');
    http.expectOne('/api/v1/peis/pei-1/versiones').flush([]);
    http.expectOne('/api/v1/pei-versiones/version-1/historial').flush([]);
    fixture.detectChanges();
    click('Crear revisión');
    expect(button('Confirmar nueva revisión')?.disabled).toBe(true);
    const reason = view.querySelector<HTMLTextAreaElement>('#pei-revision-reason')!;
    reason.value = 'Actualizar los indicadores del servicio.';
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    click('Confirmar nueva revisión');
    const draft = {
      ...pei,
      versionId: 'version-2',
      versionOrigenId: 'version-1',
    };
    const create = http.expectOne({ method: 'POST', url: '/api/v1/peis/pei-1/versiones' });
    expect(create.request.headers.get('If-Match')).toBe('"rev-9"');
    expect(create.request.body).toEqual({ motivo: 'Actualizar los indicadores del servicio.' });
    create.flush(draft, { status: 201, statusText: 'Created', headers: { ETag: '"rev-1"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1').flush(draft, { headers: { ETag: '"rev-1"' } });
    fixture.detectChanges();
    http.expectOne('/api/v1/peis/pei-1/ejes').flush([]);
    http.expectOne('/api/v1/pei-versiones/version-2/transiciones').flush({ acciones: ['enviar'] });
    const form = fixture.componentInstance.form.controls;
    expect(form.nombre.enabled).toBe(true);
    expect(form.fechaInicio.disabled).toBe(true);
    expect(form.fechaFin.disabled).toBe(true);
    expect(form.tipo.disabled).toBe(true);
  });
});
