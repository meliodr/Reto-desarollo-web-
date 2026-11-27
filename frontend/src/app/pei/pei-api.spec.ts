import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Pei, PeiApi } from './pei-api';

const example: Pei = {
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
describe('PeiApi contract', () => {
  let api: PeiApi;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(PeiApi);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('reads the list as an array', () => {
    api.list().subscribe((data) => expect(data).toEqual([example]));
    const req = http.expectOne('/api/v1/peis');
    expect(req.request.method).toBe('GET');
    req.flush([example]);
  });
  it('preserves the exact ETag returned on a detail', () => {
    api.get('pei-1').subscribe((value) => {
      expect(value.data).toEqual(example);
      expect(value.etag).toBe('"rev-1"');
    });
    http.expectOne('/api/v1/peis/pei-1').flush(example, { headers: { ETag: '"rev-1"' } });
  });
  it('creates only writable fields and returns the server version', () => {
    api.create(example).subscribe((value) => expect(value.etag).toBe('"rev-1"'));
    const req = http.expectOne('/api/v1/peis');
    expect(req.request.method).toBe('POST');
    expect(Object.keys(req.request.body).sort()).toEqual(
      ['nombre', 'tipo', 'fechaInicio', 'fechaFin', 'mision', 'vision', 'valores'].sort(),
    );
    req.flush(example, { status: 201, statusText: 'Created', headers: { ETag: '"rev-1"' } });
  });
  it('patches versionId with If-Match and propagates a stale-version conflict', () => {
    api.update('version-1', example, '"rev-1"').subscribe({
      next: () => {
        throw new Error('A conflict must not become success');
      },
      error: (error) => expect(error.status).toBe(412),
    });
    const req = http.expectOne('/api/v1/pei-versiones/version-1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.headers.get('If-Match')).toBe('"rev-1"');
    expect(req.request.body.id).toBeUndefined();
    expect(req.request.body.estado).toBeUndefined();
    req.flush({ title: 'Versión obsoleta' }, { status: 412, statusText: 'Precondition Failed' });
  });
});
