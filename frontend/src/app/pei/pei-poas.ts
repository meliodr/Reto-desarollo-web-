import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { PeiPoa, PeiWorkflowApi } from './pei-workflow-api';

@Component({
  selector: 'app-pei-poas',
  imports: [DatePipe],
  templateUrl: './pei-poas.html',
  styleUrl: './pei-poas.css',
})
export class PeiPoas {
  private readonly api = inject(PeiWorkflowApi);
  readonly peiId = input.required<string>();
  readonly poas = signal<PeiPoa[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  private readonly attempt = signal(0);

  constructor() {
    effect((cleanup) => {
      this.attempt();
      this.loading.set(true);
      this.error.set('');
      const request = this.api.poas(this.peiId()).subscribe({
        next: (poas) => {
          this.poas.set(poas);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No pudimos consultar los POA. Inténtalo de nuevo.');
          this.loading.set(false);
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }

  retry() {
    this.attempt.update((attempt) => attempt + 1);
  }

  periodLabel(state: string) {
    return state === 'fuera_de_vigencia'
      ? 'Fuera de vigencia'
      : state === 'generada'
        ? 'Generada'
        : state;
  }
}
