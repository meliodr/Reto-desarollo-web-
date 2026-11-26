import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Eje } from './pei-ejes-api';
import { PeiObjetivos } from './pei-objetivos';

const base = '/api/v1/peis/pei-1/ejes/eje-1/objetivos';
const eje: Eje = {
  id: 'eje-1',
  nombre: 'Transformación digital',
  descripcion: null,
  activo: true,
  revision: 1,
  actualizadoEn: '2026-11-13T12:00:00Z',
};
const data = {
  codigo: 'O-01',
  nombre: 'Digitalizar los servicios',
  descripcion: 'Simplificar las solicitudes de la ciudadanía.',
  resultadoEsperado: 'Cinco trámites disponibles en línea.',
  lineaAccion: 'Incorporar trámites prioritarios al portal.',
};

describe('Objetivos estratégicos en Angular', () => {
  let fixture: ComponentFixture<PeiObjetivos>;
  let http: HttpTestingController;
  let view: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PeiObjetivos],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(PeiObjetivos);
    fixture.componentRef.setInput('peiId', 'pei-1');
    http = TestBed.inject(HttpTestingController);
    view = fixture.nativeElement;
  });

  afterEach(() => http.verify());

  function render(active: boolean) {
    fixture.componentRef.setInput('eje', { ...eje, activo: active });
    fixture.detectChanges();
  }

  function click(label: string) {
    const button = [...view.querySelectorAll('button')].find((element) =>
      element.textContent?.trim().startsWith(label),
    );
    if (!button) throw new Error(`No se encontró el botón ${label}`);
    button.click();
    fixture.detectChanges();
  }

  it('exige los cinco campos y registra el objetivo bajo el eje seleccionado', () => {
    render(true);
    http.expectOne({ method: 'GET', url: base }).flush([]);
    fixture.detectChanges();

    click('Agregar objetivo');
    click('Guardar objetivo');
    http.expectNone({ method: 'POST', url: base });
    expect(view.querySelectorAll('[aria-invalid=true]')).toHaveLength(5);
    for (const [key, value] of Object.entries(data)) {
      const field = view.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#objetivo-${key}`);
      if (!field) throw new Error(`No se encontró el campo ${key}`);
      field.value = `  ${value}  `;
      field.dispatchEvent(new Event('input'));
    }

    click('Guardar objetivo');
    const create = http.expectOne({ method: 'POST', url: base });
    expect(create.request.body).toEqual(data);
    create.flush({ id: 'objetivo-1', ...data });
    fixture.detectChanges();
    for (const value of Object.values(data)) {
      expect(view.querySelector('.objetivos-list')?.textContent).toContain(value);
    }
  });

  it('permite consultar los objetivos de un eje inactivo', () => {
    render(false);
    http.expectOne({ method: 'GET', url: base }).flush([{ id: 'objetivo-1', ...data }]);
    fixture.detectChanges();
    expect(view.textContent).toContain(data.nombre);
    expect(view.textContent).toContain('Este eje está inactivo.');
    expect(view.textContent).not.toContain('Agregar objetivo');
  });

  it('conserva el formulario de indicador al actualizar los objetivos desde el mapa', () => {
    const objective = { id: 'objetivo-1', ...data, activo: true };
    render(true);
    http.expectOne(base).flush([objective]);
    fixture.detectChanges();
    click('Ver indicadores');
    http.expectOne(`${base}/objetivo-1/indicadores`).flush([]);
    fixture.detectChanges();
    click('Agregar indicador');
    const name = view.querySelector<HTMLInputElement>('#indicadores-objetivo-1-nombre')!;
    name.value = 'Un indicador sin guardar';
    name.dispatchEvent(new Event('input'));
    expect(fixture.componentInstance.hasPendingChanges()).toBe(true);

    fixture.componentRef.setInput('revision', 1);
    fixture.detectChanges();
    expect(view.querySelector('#indicadores-objetivo-1-nombre')).toBe(name);
    const update = http.expectOne(base);
    update.flush({ title: 'Error temporal' }, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(name.value).toBe('Un indicador sin guardar');
    expect(view.querySelector('fieldset[disabled]')?.contains(name)).toBe(true);

    click('Reintentar objetivos');
    http.expectOne(base).flush([{ ...objective, activo: false }]);
    fixture.detectChanges();
    expect(view.textContent).toContain('Objetivo inactivo');
    expect(view.querySelector('#indicadores-objetivo-1-nombre')).toBe(name);
    expect(name.value).toBe('Un indicador sin guardar');
    expect(view.querySelector('fieldset[disabled]')?.contains(name)).toBe(true);
  });

  it('edita con la revisión leída y conserva los cambios locales si otro actor la modifica', () => {
    const objetivo = { id: 'objetivo-1', ...data, activo: true, revision: 1 };
    const route = `${base}/objetivo-1`;
    render(true);
    http.expectOne(base).flush([objetivo]);
    fixture.detectChanges();

    click('Editar objetivo');
    http.expectOne({ method: 'GET', url: route }).flush(objetivo, {
      headers: { ETag: '"v1"' },
    });
    fixture.detectChanges();
    const field = view.querySelector<HTMLTextAreaElement>('#objetivo-resultadoEsperado')!;
    expect(field.value).toBe(data.resultadoEsperado);
    field.value = 'Atención en línea para todas las dependencias.';
    field.dispatchEvent(new Event('input'));
    click('Guardar objetivo');
    const request = http.expectOne({ method: 'PUT', url: route });
    expect(request.request.headers.get('If-Match')).toBe('"v1"');
    expect(request.request.body).toEqual({ ...data, resultadoEsperado: field.value });
    request.flush({}, { status: 412, statusText: 'Precondition Failed' });
    fixture.detectChanges();
    expect(view.textContent).toContain('La revisión cambió.');
    expect(field.value).toBe('Atención en línea para todas las dependencias.');
    expect(fixture.componentInstance.hasPendingChanges()).toBe(true);
    click('Cerrar y consultar objetivos');
    const latest = { ...objetivo, revision: 2, nombre: 'Servicios renovados' };
    http.expectOne(base).flush([latest]);
    fixture.detectChanges();
    expect(view.textContent).toContain(latest.nombre);

    click('Editar objetivo');
    http.expectOne({ method: 'GET', url: route }).flush(latest, { headers: { ETag: '"v2"' } });
    fixture.detectChanges();
    const name = view.querySelector<HTMLInputElement>('#objetivo-nombre')!;
    name.value = 'Servicios renovados y accesibles';
    name.dispatchEvent(new Event('input'));
    click('Guardar objetivo');
    const save = http.expectOne({ method: 'PUT', url: route });
    expect(save.request.headers.get('If-Match')).toBe('"v2"');
    save.flush({ ...latest, revision: 3, nombre: name.value });
    fixture.detectChanges();
    expect(view.querySelector('h5')?.textContent).toContain(name.value);
    expect(view.querySelectorAll('.objetivos-list li')).toHaveLength(1);
  });
});
