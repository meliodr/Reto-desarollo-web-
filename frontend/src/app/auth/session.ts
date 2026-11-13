import { Injectable, signal } from '@angular/core';
import Keycloak, { KeycloakConfig } from 'keycloak-js';

@Injectable({ providedIn: 'root' })
export class Session {
  private client: Keycloak | null = null;
  readonly enabled = signal(false);
  readonly authenticated = signal(false);
  readonly canPlan = signal(false);
  readonly name = signal('');
  readonly error = signal('');

  async initialize() {
    try {
      const response = await fetch('/api/configuracion', { cache: 'no-store' });
      if (!response.ok) throw new Error('Configuración no disponible');
      const { oidc: config }: { oidc: KeycloakConfig | null } = await response.json();
      if (config === null) return;

      this.enabled.set(true);
      const client = new Keycloak(config);
      this.client = client;
      client.onAuthLogout = () => this.refreshState();
      await client.init({ onLoad: 'check-sso', pkceMethod: 'S256', checkLoginIframe: false });
      this.refreshState();
    } catch {
      this.enabled.set(true);
      this.error.set('No se pudo iniciar el acceso. Recarga la página para reintentar.');
    }
  }

  async login() {
    try {
      await this.client?.login();
    } catch {
      this.error.set('No se pudo abrir el inicio de sesión. Recarga la página para reintentar.');
    }
  }

  async logout() {
    try {
      await this.client?.logout({ redirectUri: new URL('/peis', location.origin).href });
    } catch {
      this.error.set('No se pudo cerrar la sesión. Vuelve a intentarlo.');
    }
  }

  async accessToken() {
    if (!this.client?.authenticated) return undefined;
    try {
      await this.client.updateToken(30);
      return this.client.token;
    } catch {
      this.client.clearToken();
      this.refreshState();
      throw new Error('La sesión expiró. Vuelve a iniciar sesión.');
    }
  }

  private refreshState() {
    const active = this.client?.authenticated === true;
    this.authenticated.set(active);
    this.canPlan.set(active && this.client?.hasResourceRole('Planificador') === true);
    const username = this.client?.tokenParsed?.['preferred_username'];
    this.name.set(typeof username === 'string' ? username : '');
  }
}
