import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { PeiForm } from './pei-form';
import { Pei, PeiApi, VersionedPei } from './pei-api';
import { Feedback } from './feedback';

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
describe('PeiForm save failure recovery', () => {
  let component: PeiForm;
  const api = { get: vi.fn(), update: vi.fn(), create: vi.fn() };
  beforeEach(() => {
    api.get.mockReturnValue(of({ data: { ...example }, etag: '"rev-1"' }));
    api.update.mockReset();
    api.create.mockReset();
    TestBed.configureTestingModule({
      imports: [PeiForm],
      providers: [
        provideRouter([]),
        { provide: PeiApi, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'pei-1' }) } },
        },
      ],
    });
    component = TestBed.createComponent(PeiForm).componentInstance;
    component.ngOnInit();
  });
  it('keeps the local draft and old ETag on 412 without announcing success', () => {
    component.form.controls.mision.setValue('Edición de esta sesión.');
    component.form.markAsDirty();
    api.update.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: 412, error: { title: 'Versión obsoleta' } }),
      ),
    );
    component.submit();
    expect(component.form.controls.mision.value).toBe('Edición de esta sesión.');
    expect(component.form.dirty).toBe(true);
    expect(component.etag()).toBe('"rev-1"');
    expect(component.conflict()).toBe(true);
    expect(component.saving()).toBe(false);
    expect(TestBed.inject(Feedback).message()).toBe('');
  });
  it('maps 422 errors to the field and preserves values', () => {
    api.update.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 422,
            error: { errors: { nombre: ['Ese nombre necesita revisión.'] } },
          }),
      ),
    );
    component.submit();
    expect(component.fieldError('nombre')).toBe('Ese nombre necesita revisión.');
    expect(component.form.controls.nombre.value).toBe(example.nombre);
    expect(component.saved()).toBe(false);
  });
  it('locks the fields while the save is pending and unlocks after the response', () => {
    const response = new Subject<VersionedPei>();
    api.update.mockReturnValue(response);
    component.submit();
    expect(component.form.disabled).toBe(true);
    response.next({ data: example, etag: '"rev-2"' });
    response.complete();
    expect(component.form.enabled).toBe(true);
    expect(component.saved()).toBe(true);
  });
  it('fetching a newer version does not silently replace local changes', () => {
    component.form.controls.vision.setValue('Mi visión aún no guardada.');
    api.get.mockReturnValue(
      of({ data: { ...example, vision: 'Cambio de otra sesión.' }, etag: '"rev-2"' }),
    );
    component.fetchLatest();
    expect(component.form.controls.vision.value).toBe('Mi visión aún no guardada.');
    expect(component.etag()).toBe('"rev-1"');
    expect(component.differences().find((d) => d.field === 'vision')?.server).toBe(
      'Cambio de otra sesión.',
    );
  });
});
