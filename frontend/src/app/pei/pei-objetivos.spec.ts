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
});
