import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PeiIndicadores } from './pei-indicadores';
import { Indicador } from './pei-indicadores-api';

const url = '/api/v1/peis/pei-1/ejes/eje-1/objetivos/objetivo-1/indicadores';

function abrirIndicadores() {
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
  return { fixture, http: TestBed.inject(HttpTestingController) };
}

it('guarda un indicador y vuelve a consultar la medición registrada', () => {
  const { fixture, http } = abrirIndicadores();
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
    sentido: 'ascendente',
    metodoCalculo: 'valor_directo',
  });
  component.saveIndicator();

  const indicator: Indicador = {
    id: 'indicador-1',
    nombre: 'Servicios digitalizados',
    formula: 'Total de trámites en línea',
    unidad: 'trámites',
    lineaBase: '999999999999.0003',
    meta: '10',
    frecuencia: 'Cuatrimestral',
    sentido: 'ascendente',
    metodoCalculo: 'valor_directo',
    listoParaMedir: true,
    revision: 1,
  };
  const create = http.expectOne({ method: 'POST', url });
  const { id, ...data } = indicator;
  expect(create.request.body).toEqual({
    nombre: data.nombre,
    formula: data.formula,
    unidad: data.unidad,
    lineaBase: data.lineaBase,
    meta: data.meta,
    frecuencia: data.frecuencia,
    sentido: data.sentido,
    metodoCalculo: data.metodoCalculo,
  });
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

const importado: Indicador = {
  id: 'indicador-importado',
  nombre: 'Tiempo de respuesta',
  formula: null,
  unidad: null,
  frecuencia: null,
  meta: null,
  lineaBase: null,
  sentido: null,
  metodoCalculo: null,
  listoParaMedir: false,
  revision: 1,
};

it('bloquea mediciones pendientes y activa un indicador importado con ETag', () => {
  const { fixture, http } = abrirIndicadores();
  const component = fixture.componentInstance;
  const view: HTMLElement = fixture.nativeElement;
  const endpoint = `${url}/${importado.id}`;
  http.expectOne({ method: 'GET', url }).flush([importado]);
  component.select(importado);
  http.expectOne({ method: 'GET', url: `${endpoint}/mediciones` }).flush([]);
  fixture.detectChanges();
  expect(view.textContent).toContain('Pendiente de configuración');
  expect(view.textContent).toContain(
    'La captura de mediciones se habilita al completar la configuración.',
  );
  expect(view.querySelector('input[type="date"]')).toBeNull();

  component.startConfigure(importado);
  http.expectOne({ method: 'GET', url: endpoint }).flush(importado, { headers: { ETag: '"v1"' } });
  expect(component.form.controls.nombre.value).toBe(importado.nombre);
  component.form.patchValue({
    formula: 'Tiempo total dividido entre las solicitudes atendidas',
    unidad: 'horas',
    frecuencia: 'Mensual',
    meta: '2,5',
    sentido: 'descendente',
    metodoCalculo: 'variacion_porcentual',
  });
  component.saveIndicator();
  expect(component.form.hasError('lineaBaseRequerida')).toBe(true);
  component.form.controls.lineaBase.setValue('0,00');
  component.saveIndicator();
  expect(component.form.hasError('lineaBaseCero')).toBe(true);
  http.expectNone({ method: 'PUT', url: endpoint });

  component.form.patchValue({ metodoCalculo: 'valor_directo', lineaBase: '' });
  component.saveIndicator();
  const put = http.expectOne({ method: 'PUT', url: endpoint });
  expect(put.request.headers.get('If-Match')).toBe('"v1"');
  expect(put.request.body).toMatchObject({
    meta: '2.5',
    lineaBase: null,
    metodoCalculo: 'valor_directo',
  });
  put.flush({ ...importado, ...put.request.body, revision: 2, listoParaMedir: true });
  fixture.detectChanges();
  expect(view.textContent).toContain('Listo para medir');
  expect(view.textContent).toContain('Valor directo');
  http.verify();
});

it('conserva los datos escritos al detectar una revisión obsoleta y recarga el listado al cerrar', () => {
  const { fixture, http } = abrirIndicadores();
  const component = fixture.componentInstance;
  const indicador: Indicador = {
    ...importado,
    formula: 'Número de servicios',
    unidad: 'servicios',
    frecuencia: 'Mensual',
    meta: '5',
    sentido: 'ascendente',
    metodoCalculo: 'valor_directo',
    listoParaMedir: true,
  };
  const endpoint = `${url}/${indicador.id}`;
  http.expectOne({ method: 'GET', url }).flush([indicador]);
  component.startConfigure(indicador);
  http.expectOne({ method: 'GET', url: endpoint }).flush(indicador, { headers: { ETag: '"v1"' } });
  component.form.controls.meta.setValue('6');
  component.saveIndicator();
  http
    .expectOne({ method: 'PUT', url: endpoint })
    .flush({}, { status: 412, statusText: 'Precondition Failed' });
  expect(component.form.controls.meta.value).toBe('6');
  expect(component.configurationChanged()).toBe(true);
  component.saveIndicator();
  http.expectNone({ method: 'PUT', url: endpoint });
  component.cancelIndicator();
  http.expectOne({ method: 'GET', url }).flush([{ ...indicador, meta: '7', revision: 2 }]);
  expect(component.indicadores()[0]?.meta).toBe('7');
  http.verify();
});
