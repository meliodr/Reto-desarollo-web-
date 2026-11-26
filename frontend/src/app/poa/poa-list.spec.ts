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
  expect(request.request.body).toEqual({ anio: 2026, peiId: null });
  request.flush({
    id: 'poa-1',
    anio: 2026,
    estado: 'formulacion',
    fechaInicio: '2026-01-01',
    fechaFin: '2026-12-31',
    peiId: null,
    peiNombre: null,
  });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('Independiente');
  expect(component.showForm()).toBe(false);
  http.verify();
});
