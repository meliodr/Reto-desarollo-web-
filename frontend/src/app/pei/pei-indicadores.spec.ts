import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PeiIndicadores } from './pei-indicadores';

const url = '/api/v1/peis/pei-1/ejes/eje-1/objetivos/objetivo-1/indicadores';

it('guarda un indicador y vuelve a consultar la medición registrada', () => {
  TestBed.configureTestingModule({
    imports: [PeiIndicadores],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(PeiIndicadores);
  fixture.componentRef.setInput('ruta', {
    peiId: 'pei-1',
    ejeId: 'eje-1',
    objetivoId: 'objetivo-1',
  });
  fixture.detectChanges();

  const http = TestBed.inject(HttpTestingController);
  http.expectOne({ method: 'GET', url }).flush([]);
  const component = fixture.componentInstance;
  component.startCreate();
  component.form.setValue({
    nombre: 'Servicios digitalizados',
    formula: 'Total de trámites en línea',
    unidad: 'trámites',
    lineaBase: '999999999999,0003',
    meta: '10',
    frecuencia: 'Cuatrimestral',
  });
  component.saveIndicator();

  const indicator = {
    id: 'indicador-1',
    nombre: 'Servicios digitalizados',
    formula: 'Total de trámites en línea',
    unidad: 'trámites',
    lineaBase: '999999999999.0003',
    meta: '10',
    frecuencia: 'Cuatrimestral',
  };
  const create = http.expectOne({ method: 'POST', url });
  const { id, ...payload } = indicator;
  expect(create.request.body).toEqual(payload);
  create.flush(indicator);

  const measurements = `${url}/${id}/mediciones`;
  component.select(indicator);
  http.expectOne({ method: 'GET', url: measurements }).flush([]);
  component.medicionForm.setValue({ periodo: '2026-04-30', valor: '999999999999.0003' });
  component.saveMeasurement(indicator);
  const request = http.expectOne({ method: 'POST', url: measurements });
  const saved = { id: 'medicion-1', periodo: '2026-04-30', valor: '999999999999.0003' };
  expect(request.request.body).toEqual({ periodo: saved.periodo, valor: saved.valor });
  request.flush(saved);
  http.expectOne({ method: 'GET', url: measurements }).flush([saved]);
  fixture.detectChanges();

  const view: HTMLElement = fixture.nativeElement;
  expect(view.querySelector('time')?.textContent).toBe(saved.periodo);
  expect(view.querySelector('tbody')?.textContent).toContain('999999999999,0003');
  http.verify();
});
