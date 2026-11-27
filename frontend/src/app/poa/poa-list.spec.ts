import { activateAnalyst } from '../../testing/membresia';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PoaList } from './poa-list';
import { Session } from '../auth/session';

const areas = [
  { id: 'area-1', nombre: 'Planificación', activa: true },
  { id: 'area-2', nombre: 'Tecnología', activa: true },
  { id: 'area-3', nombre: 'Archivo', activa: false },
];

it('creates an independent POA and shows a linked plan only for a covered year', () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst();
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  const component = fixture.componentInstance;
  http.expectOne('/api/v1/organizacion').flush({ dependencias: areas });
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/peis?oficiales=true').flush([
    {
      id: 'publicado',
      nombre: 'Plan estratégico',
      estado: 'publicado',
      fechaInicio: '2026-03-01',
      fechaFin: '2029-01-12',
    },
    {
      id: 'borrador',
      nombre: 'Pendiente',
      estado: 'borrador',
      fechaInicio: '2026-03-01',
      fechaFin: '2029-01-12',
    },
  ]);
  component.year.set(2025);
  expect(component.candidates()).toEqual([]);
  component.year.set(2026);
  expect(component.candidates().map((plan) => plan.id)).toEqual(['publicado']);
  component.showForm.set(true);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('#poa-pei option').textContent).toBe(
    'POA independiente',
  );

  component.save();
  const request = http.expectOne({ method: 'POST', url: '/api/v1/poas' });
  expect(request.request.body).toEqual({
    anio: 2026,
    peiId: null,
    presupuestoReferencia: null,
    dependenciaId: null,
  });
  request.flush({
    id: 'poa-1',
    anio: 2026,
    estado: 'formulacion',
    fechaInicio: '2026-01-01',
    fechaFin: '2026-12-31',
    peiId: null,
    peiNombre: null,
    presupuestoReferencia: null,
  });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Independiente');
  expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Sin registrar');
  expect(component.showForm()).toBe(false);
  http.verify();
});

it('validates the optional budget and saves cents exactly with a decimal comma', async () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst();
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/organizacion').flush({ dependencias: areas });
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/peis?oficiales=true').flush([]);
  fixture.componentInstance.showForm.set(true);
  fixture.componentInstance.year.set(2027);
  fixture.detectChanges();
  await fixture.whenStable();
  const input: HTMLInputElement = fixture.nativeElement.querySelector('#poa-budget');
  const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
  input.value = '1200,123';
  input.dispatchEvent(new Event('input'));
  form.dispatchEvent(new Event('submit'));
  fixture.detectChanges();
  http.expectNone({ method: 'POST', url: '/api/v1/poas' });
  expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
    '2 decimales',
  );

  input.value = '999999999999,03';
  input.dispatchEvent(new Event('input'));
  form.dispatchEvent(new Event('submit'));
  const request = http.expectOne({ method: 'POST', url: '/api/v1/poas' });
  expect(request.request.body.presupuestoReferencia).toBe('999999999999.03');
  request.flush({
    id: 'poa-2',
    anio: 2027,
    estado: 'borrador',
    fechaInicio: '2027-01-01',
    fechaFin: '2027-12-31',
    peiId: null,
    peiNombre: null,
    presupuestoReferencia: '999999999999.03',
  });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('RD$ 999999999999.03');
  http.verify();
});

it('abre un POA autónomo sin consultar ni ofrecer el módulo PEI deshabilitado', () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst(false, true);
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/organizacion').flush({ dependencias: areas });
  http.expectOne('/api/v1/poas').flush([]);
  http.expectNone('/api/v1/peis?oficiales=true');
  fixture.componentInstance.showForm.set(true);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('#poa-pei')).toBeNull();
  expect(fixture.nativeElement.querySelector('#poa-year')).not.toBeNull();
  http.verify();
});

it('muestra las dependencias activas y guarda la elegida para el POA', async () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst(false, true);
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/organizacion').flush({ dependencias: areas });
  const component = fixture.componentInstance;
  component.showForm.set(true);
  fixture.detectChanges();
  await fixture.whenStable();

  const select: HTMLSelectElement = fixture.nativeElement.querySelector('#poa-dependencia');
  expect(Array.from(select.options, (option) => option.value)).toEqual(['', 'area-1', 'area-2']);
  select.value = 'area-2';
  select.dispatchEvent(new Event('change'));
  component.year.set(2230);
  component.save();
  const request = http.expectOne({ method: 'POST', url: '/api/v1/poas' });
  expect(request.request.body.dependenciaId).toBe('area-2');
  request.flush({ anio: 2230, dependenciaId: 'area-2', id: 'poa-area', estado: 'borrador' });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Tecnología');
  expect(component.dependenciaElegida()).toBe('');
  component.showForm.set(true);
  component.dependenciaElegida.set('area-1');
  component.cerrarFormulario();
  expect(component.dependenciaElegida()).toBe('');
  http.verify();
});

it('conserva la dependencia del responsable al reintentar el catálogo', () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst(false, true);
  const session = TestBed.inject(Session);
  session.active.set({ ...session.active()!, rol: 'ResponsableArea', dependenciaId: 'area-1' });
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/organizacion').flush({}, { status: 503, statusText: 'Unavailable' });
  fixture.componentInstance.showForm.set(true);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('en tu área');
  fixture.nativeElement.querySelector('.text-button').click();
  http.expectOne('/api/v1/organizacion').flush({ dependencias: [areas[0]] });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('#poa-dependencia')).toBeNull();
  expect(fixture.nativeElement.querySelector('form').textContent).toContain('Planificación');
  fixture.componentInstance.save();
  const request = http.expectOne({ method: 'POST', url: '/api/v1/poas' });
  expect(request.request.body.dependenciaId).toBe('area-1');
  request.flush({ anio: 2230, dependenciaId: 'area-1', id: 'poa-area', estado: 'borrador' });
  http.verify();
});

it('explica el año inválido, conserva el borrador y permite corregirlo antes de guardar', async () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  activateAnalyst();
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/organizacion').flush({ dependencias: areas });
  http.expectOne('/api/v1/peis').flush([
    {
      id: 'pei-vigente',
      nombre: 'Plan estratégico',
      estado: 'publicado',
      fechaInicio: '2026-01-01',
      fechaFin: '2029-12-31',
    },
  ]);
  const component = fixture.componentInstance;
  component.year.set(2027);
  component.peiId.set('pei-vigente');
  component.dependenciaElegida.set('area-2');
  component.budget.set('125,50');
  component.showForm.set(true);
  fixture.detectChanges();
  await fixture.whenStable();
  const view: HTMLElement = fixture.nativeElement;
  const year = view.querySelector<HTMLInputElement>('#poa-year')!;
  const submit = view.querySelector<HTMLButtonElement>('[type="submit"]')!;
  const form = view.querySelector('form')!;
  for (const value of ['', '0', '10000', '2027.5']) {
    year.value = value;
    year.dispatchEvent(new Event('input'));
    year.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    const error = view.querySelector<HTMLElement>('#poa-year-error')!;
    expect(year.getAttribute('aria-describedby')).toBe(error.id);
    expect(year.getAttribute('aria-invalid')).toBe('true');
    expect(error.textContent).toContain(value ? 'entero' : 'año del POA');
    expect(submit.disabled).toBe(true);
    form.dispatchEvent(new Event('submit'));
    http.expectNone({ method: 'POST', url: '/api/v1/poas' });
    expect(component.showForm()).toBe(true);
    expect(component.peiId()).toBe('pei-vigente');
  }
  year.value = '2028';
  year.dispatchEvent(new Event('input'));
  year.dispatchEvent(new Event('blur'));
  fixture.detectChanges();
  expect(view.querySelector('#poa-year-error')).toBeNull();
  expect(year.hasAttribute('aria-invalid')).toBe(false);
  expect(submit.disabled).toBe(false);
  submit.click();
  const request = http.expectOne({ method: 'POST', url: '/api/v1/poas' });
  expect(request.request.body).toEqual({
    anio: 2028,
    peiId: 'pei-vigente',
    dependenciaId: 'area-2',
    presupuestoReferencia: '125.50',
  });
  request.flush({ id: 'poa-prueba', anio: 2028 });
  expect(component.showForm()).toBe(false);
  http.verify();
});
