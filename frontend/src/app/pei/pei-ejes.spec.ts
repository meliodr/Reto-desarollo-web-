import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Eje } from './pei-ejes-api';
import { PeiEjes } from './pei-ejes';

const base = '/api/v1/peis/pei-1/ejes';
const eje: Eje = {
  id: 'eje-1',
  nombre: 'Transformación digital',
  descripcion: null,
  activo: true,
  revision: 1,
  actualizadoEn: '2026-11-13T12:00:00Z',
};

describe('Ejes estratégicos en Angular', () => {
  let fixture: ComponentFixture<PeiEjes>;
  let http: HttpTestingController;
  let view: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PeiEjes],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(PeiEjes);
    fixture.componentRef.setInput('peiId', 'pei-1');
    http = TestBed.inject(HttpTestingController);
    view = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  function click(label: string) {
    const button = [...view.querySelectorAll('button')].find((element) =>
      element.textContent?.trim().startsWith(label),
    );
    if (!button) throw new Error(`No se encontró el botón ${label}`);
    button.click();
    fixture.detectChanges();
  }

  function fill(name: string) {
    const field = view.querySelector<HTMLInputElement>('#eje-nombre');
    if (!field) throw new Error('No se encontró el campo de nombre');
    field.value = name;
    field.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  it('crea, edita e inactiva un eje y muestra las revisiones anteriores', () => {
    http.expectOne({ method: 'GET', url: base }).flush([]);
    fixture.detectChanges();
    click('Agregar eje');
    fill('  Transformación digital ');
    click('Guardar eje');
    const create = http.expectOne({ method: 'POST', url: base });
    expect(create.request.body).toEqual({ nombre: eje.nombre, descripcion: null });
    create.flush(eje);
    fixture.detectChanges();

    click('Editar');
    http.expectOne({ method: 'GET', url: `${base}/eje-1` }).flush(eje, {
      headers: { ETag: '"primera-revision"' },
    });
    fixture.detectChanges();
    fill('Servicios digitales');
    click('Guardar eje');
    const update = http.expectOne({ method: 'PUT', url: `${base}/eje-1` });
    expect(update.request.headers.get('If-Match')).toBe('"primera-revision"');
    expect(update.request.body).toEqual({
      nombre: 'Servicios digitales',
      descripcion: null,
      activo: true,
      revision: 1,
    });
    const edited = { ...eje, nombre: 'Servicios digitales', revision: 2 };
    update.flush(edited);
    fixture.detectChanges();

    click('Editar');
    http.expectOne({ method: 'GET', url: `${base}/eje-1` }).flush(edited, {
      headers: { ETag: '"segunda-revision"' },
    });
    fixture.detectChanges();
    click('Inactivar eje');
    click('Confirmar inactivación');
    const disable = http.expectOne({ method: 'PUT', url: `${base}/eje-1` });
    expect(disable.request.body).toMatchObject({ activo: false, revision: 2 });
    const inactive = { ...edited, activo: false, revision: 3 };
    disable.flush(inactive);
    fixture.detectChanges();

    click('Ver historial');
    http
      .expectOne({ method: 'GET', url: `${base}/eje-1/versiones` })
      .flush([inactive, edited, eje]);
    fixture.detectChanges();
    expect(view.textContent).toContain('Versión 3 · Inactivo');
    expect(view.querySelector('.eje-history')?.textContent).toContain(
      'Versión 1 · Transformación digital',
    );
  });

  it('conserva el borrador del eje si otra sesión lo cambió y exige volver a consultar', () => {
    http.expectOne({ method: 'GET', url: base }).flush([eje]);
    fixture.detectChanges();
    click('Editar');
    http.expectOne({ method: 'GET', url: `${base}/eje-1` }).flush(eje, {
      headers: { ETag: '"primera-revision"' },
    });
    fixture.detectChanges();
    fill('Mi edición');
    click('Guardar eje');
    http
      .expectOne({ method: 'PUT', url: `${base}/eje-1` })
      .flush({ title: 'Versión obsoleta' }, { status: 412, statusText: 'Precondition Failed' });
    fixture.detectChanges();

    expect(view.querySelector<HTMLInputElement>('#eje-nombre')?.value).toBe('Mi edición');
    expect(view.querySelector<HTMLButtonElement>('button[type=submit]')?.disabled).toBe(true);
    expect(view.querySelector('[role=alert]')?.textContent).toContain('cambió en otra sesión');
    expect(fixture.componentInstance.hasPendingChanges()).toBe(true);

    click('Cerrar y consultar listado');
    http
      .expectOne({ method: 'GET', url: base })
      .flush([{ ...eje, nombre: 'Edición externa', revision: 2 }]);
    fixture.detectChanges();
    expect(view.textContent).toContain('Edición externa');
  });
});
