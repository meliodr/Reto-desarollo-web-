import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Institucion, Session } from './session';
import { sessionInterceptor } from './session.interceptor';

const a: Institucion = {
  id: '00000000-0000-0000-0000-000000000001',
  nombre: 'Institución A',
  rol: 'AnalistaPlanificacion',
  dependenciaId: null,
  modulos: { pei: true, poa: true },
};
const b: Institucion = {
  id: '00000000-0000-0000-0000-000000000002',
  nombre: 'Institución B',
  rol: 'Auditor',
  dependenciaId: null,
  modulos: { pei: false, poa: true },
};

describe('Contexto institucional', () => {
  let session: Session;
  let http: HttpClient;
  let requests: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([sessionInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    session = TestBed.inject(Session);
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
    vi.spyOn(session, 'accessToken').mockResolvedValue('token-de-prueba');
  });
  afterEach(() => {
    requests.verify();
    vi.unstubAllGlobals();
  });

  it('verifica el cambio, cancela respuestas anteriores y usa la membresía activa', async () => {
    session.active.set(a);
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(Response.json({ instituciones: [a, b], institucionActivaId: b.id })),
    );
    const stale = vi.fn();
    http.get('/api/v1/peis').subscribe(stale);
    await Promise.resolve();
    const pending = requests.expectOne('/api/v1/peis');
    expect(pending.request.headers.get('X-Institucion-Id')).toBe(a.id);
    expect(pending.request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
    expect(session.canPlan()).toBe(true);
    await session.selectInstitution(b.id);
    expect(pending.cancelled).toBe(true);
    expect(stale).not.toHaveBeenCalled();
    const headers = vi.mocked(fetch).mock.calls[0][1]?.headers as Headers;
    expect(headers.get('X-Institucion-Id')).toBe(b.id);
    expect(session.canAudit()).toBe(true);
    expect(session.canPlan()).toBe(false);
    expect(session.canFormulatePoa()).toBe(false);
    expect(session.home()).toBe('/poas');
    http.get('/api/v1/poas').subscribe();
    await Promise.resolve();
    const next = requests.expectOne('/api/v1/poas');
    expect(next.request.headers.get('X-Institucion-Id')).toBe(b.id);
    next.flush([]);
  });

  it('borra el contexto anterior cuando la API deniega la pertenencia', async () => {
    session.active.set(a);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 403 })));
    await session.selectInstitution(b.id);
    expect(session.active()).toBeNull();
    expect(session.canPlan()).toBe(false);
    expect(session.canAudit()).toBe(false);
    const denied = vi.fn();
    http.get('/api/v1/peis').subscribe({ error: denied });
    expect(denied).toHaveBeenCalledWith(expect.objectContaining({ status: 403 }));
    requests.expectNone('/api/v1/peis');
  });

  it('retira la institución y sus permisos si la API informa que expiró la sesión', async () => {
    session.active.set(a);
    session.authenticated.set(true);
    http.get('/api/v1/peis').subscribe({ error: () => {} });
    await Promise.resolve();
    requests.expectOne('/api/v1/peis').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(session.authenticated()).toBe(false);
    expect(session.active()).toBeNull();
    expect(session.canRead()).toBe(false);
    expect(session.error()).toContain('expiró');
  });
});
