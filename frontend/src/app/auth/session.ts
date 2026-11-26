import { Injectable, computed, signal } from '@angular/core';
import Keycloak, { KeycloakConfig } from 'keycloak-js';
import { Subject } from 'rxjs';

export interface Institucion {
  id: string;
  nombre: string;
  rol: string;
  dependenciaId: string | null;
  modulos: { pei: boolean; poa: boolean };
}

interface SesionInstitucional {
  instituciones: Institucion[];
  institucionActivaId: string;
}

@Injectable({ providedIn: 'root' })
export class Session {
  private client: Keycloak | null = null;
  private readonly contextChanges = new Subject<void>();
  private revision = 0;
  readonly contextChanged = this.contextChanges.asObservable();
  readonly authenticated = signal(false);
  readonly institutions = signal<Institucion[]>([]);
  readonly active = signal<Institucion | null>(null);
  readonly loading = signal(false);
  readonly name = signal('');
  readonly error = signal('');
  readonly canRead = computed(() => this.active() !== null);
  readonly pei = computed(() => this.active()?.modulos.pei === true);
  readonly poa = computed(() => this.active()?.modulos.poa === true);
  readonly canAudit = computed(() => {
    const role = this.active()?.rol;
    return role === 'Auditor' || role === 'AdministradorInstitucional';
  });
  private readonly isAnalyst = computed(() => {
    const role = this.active()?.rol;
    return role === 'AnalistaPlanificacion' || role === 'Planificador';
  });
  readonly canPlan = computed(() => this.pei() && this.isAnalyst());
  readonly canFormulatePoa = computed(
    () =>
      this.poa() &&
      (this.isAnalyst() ||
        this.active()?.rol === 'DirectorPlanificacion' ||
        this.active()?.rol === 'ResponsableArea'),
  );
  readonly home = computed(() => (this.pei() ? '/peis' : this.poa() ? '/poas' : '/institucion'));

  async initialize() {
    try {
      const response = await fetch('/api/configuracion', { cache: 'no-store' });
      if (!response.ok) throw new Error();
      const { oidc: config }: { oidc: KeycloakConfig | null } = await response.json();
      if (!config) throw new Error();

      const client = new Keycloak(config);
      this.client = client;
      client.onAuthLogout = () => this.clearSession();
      client.onTokenExpired = () => {
        void this.accessToken().catch(() => {});
      };
      await client.init({ onLoad: 'check-sso', pkceMethod: 'S256', checkLoginIframe: false });
      this.authenticated.set(client.authenticated === true);
      if (!this.authenticated()) return;
      this.name.set(client.tokenParsed?.['preferred_username'] ?? '');
      let preferred: string | undefined;
      try {
        preferred = sessionStorage.getItem(this.preferenceKey()) ?? undefined;
      } catch {
        // La sesión también funciona con el almacenamiento del navegador desactivado.
      }
      await this.selectInstitution(preferred);
    } catch {
      this.clearSession();
      this.error.set(
        'No se pudo iniciar el acceso institucional. Recarga la página para reintentar.',
      );
    }
  }

  async selectInstitution(id?: string) {
    const revision = ++this.revision;
    this.contextChanges.next();
    this.active.set(null);
    this.error.set('');
    this.loading.set(true);
    try {
      const headers = new Headers({ Authorization: 'Bearer ' + (await this.accessToken()) });
      if (id) headers.set('X-Institucion-Id', id);
      const response = await fetch('/api/v1/sesion', { headers, cache: 'no-store' });
      if (revision !== this.revision) return;
      if (response.status === 401) {
        this.expire();
        return;
      }
      if (response.status === 403) {
        this.institutions.set([]);
        if (id) this.error.set('Esta cuenta ya no tiene acceso a la institución seleccionada.');
        return;
      }
      if (!response.ok) throw new Error();
      const data: SesionInstitucional = await response.json();
      if (revision !== this.revision) return;
      const current = data.instituciones.find((item) => item.id === data.institucionActivaId);
      if (!current || (id && current.id !== id)) throw new Error();
      this.institutions.set(data.instituciones);
      this.active.set(current);
      try {
        sessionStorage.setItem(this.preferenceKey(), current.id);
      } catch {
        // El servidor ya confirmó el contexto para esta página.
      }
    } catch {
      if (revision === this.revision) {
        this.error.set(
          'No se pudo verificar la institución activa. Vuelve a consultar tus accesos.',
        );
      }
    } finally {
      if (revision === this.revision) this.loading.set(false);
    }
  }

  async login() {
    try {
      if (!this.client) {
        location.reload();
        return;
      }
      await this.client.login();
    } catch {
      this.error.set('No se pudo abrir el inicio de sesión. Recarga la página para reintentar.');
    }
  }

  async logout() {
    this.clearSession();
    try {
      await this.client?.logout({ redirectUri: new URL('/', location.origin).href });
    } catch {
      this.error.set(
        'No se pudo cerrar la sesión institucional. Recarga la página para reintentar.',
      );
    }
  }

  async accessToken(): Promise<string> {
    if (this.client?.authenticated) {
      try {
        await this.client.updateToken(30);
        if (this.client.token) return this.client.token;
      } catch {
        // Keycloak puede invalidar el token durante la renovación.
      }
    }
    this.expire();
    throw new Error('La sesión expiró. Vuelve a iniciar sesión.');
  }

  expire() {
    this.client?.clearToken();
    this.clearSession();
    this.error.set('Tu sesión expiró. Vuelve a iniciar sesión para continuar.');
  }

  private preferenceKey() {
    return 'sisplan.institucion.' + this.client?.tokenParsed?.iss + '.' + this.client?.subject;
  }

  private clearSession() {
    this.revision++;
    this.contextChanges.next();
    this.active.set(null);
    this.institutions.set([]);
    this.name.set('');
    this.authenticated.set(false);
    this.loading.set(false);
  }
}
