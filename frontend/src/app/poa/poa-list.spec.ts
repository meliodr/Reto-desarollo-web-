import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PoaList } from './poa-list';

it('creates an independent POA and shows a linked plan only for a covered year', () => {
  TestBed.configureTestingModule({
    imports: [PoaList],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  const component = fixture.componentInstance;
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/peis').flush([
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
  expect(request.request.body).toEqual({ anio: 2026, peiId: null, presupuestoReferencia: null });
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
  const fixture = TestBed.createComponent(PoaList);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/v1/poas').flush([]);
  http.expectOne('/api/v1/peis').flush([]);
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
