import { Component, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Session } from './auth/session';
import { Feedback } from './pei/feedback';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly session = inject(Session);
  readonly feedback = inject(Feedback);
  readonly switchingInstitution = signal(false);
  private readonly router = inject(Router);

  readonly collapsed = signal(false);
  readonly dark = signal(false);

  constructor() {
    try { this.dark.set(localStorage.getItem('sisplan.theme') === 'dark'); } catch {}
    effect(() => {
      const dark = this.dark();
      document.documentElement.dataset['theme'] = dark ? 'dark' : 'light';
      try { localStorage.setItem('sisplan.theme', dark ? 'dark' : 'light'); } catch {}
    });
    if (typeof matchMedia !== 'undefined' && matchMedia('(max-width: 760px)').matches) {
      this.collapsed.set(true);
    }
  }

  async switchInstitution(select: HTMLSelectElement) {
    const id = select.value;
    const previous = this.session.active()?.id;
    if (!id || previous === id || this.switchingInstitution()) return;
    const previousModule = this.router.url.split(/[/?#]/)[1];
    this.switchingInstitution.set(true);
    try {
      // Conserva la selección solicitada mientras la navegación consulta cambios pendientes.
      if (!(await this.router.navigateByUrl('/institucion')) && this.router.url !== '/institucion') {
        select.value = previous ?? '';
        return;
      }
      this.feedback.message.set('');
      await this.session.selectInstitution(id);
      if (this.session.active()?.id !== id) return;
      const target = previousModule === 'poas' && this.session.poa() ? '/poas' : this.session.home();
      await this.router.navigateByUrl(target, { replaceUrl: true });
    } finally {
      this.switchingInstitution.set(false);
    }
  }
}
