import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Session } from './auth/session';
import { Feedback } from './pei/feedback';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly session = inject(Session);
  readonly feedback = inject(Feedback);
  private readonly router = inject(Router);

  async switchInstitution(select: HTMLSelectElement) {
    const id = select.value;
    select.value = this.session.active()?.id ?? '';
    if (!id || this.session.active()?.id === id) return;
    const previousModule = this.router.url.split(/[/?#]/)[1];
    // Desmonta la vista anterior y respeta el aviso de cambios pendientes.
    if (!(await this.router.navigateByUrl('/institucion')) && this.router.url !== '/institucion')
      return;
    this.feedback.message.set('');
    await this.session.selectInstitution(id);
    if (this.session.active()?.id !== id) return;
    const target = previousModule === 'poas' && this.session.poa() ? '/poas' : this.session.home();
    await this.router.navigateByUrl(target, { replaceUrl: true });
  }
}
