import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Pei, PeiApi, PeiState, peiStateLabels } from './pei-api';
import { Session } from '../auth/session';
@Component({ imports: [RouterLink], templateUrl: './pei-list.html' })
export class PeiList {
  private readonly api = inject(PeiApi);
  private readonly retry = signal(0);
  readonly session = inject(Session);
  readonly stateLabels = peiStateLabels;
  readonly peis = signal<Pei[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly query = signal('');
  readonly type = signal('todos');
  readonly state = signal<PeiState | 'todos'>('todos');
  readonly officialOnly = computed(() => this.state() === 'publicado');
  readonly states = Object.keys(peiStateLabels) as PeiState[];
  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('es');
    const year = /^\d{4}$/.test(query) ? Number(query) : null;
    return this.peis().filter((p) => {
      const matches =
        year === null
          ? `${p.nombre} ${p.fechaInicio} ${p.fechaFin}`.toLocaleLowerCase('es').includes(query)
          : year >= Number(p.fechaInicio.slice(0, 4)) && year <= Number(p.fechaFin.slice(0, 4));
      return (
        matches &&
        (this.type() === 'todos' || p.tipo === this.type()) &&
        (this.state() === 'todos' || p.estado === this.state())
      );
    });
  });
  constructor() {
    effect((cleanup) => {
      const officialOnly = this.officialOnly();
      this.retry();
      this.loading.set(true);
      this.error.set('');
      const request = this.api.list(officialOnly).subscribe({
        next: (data) => {
          this.peis.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No pudimos consultar los planes. Vuelve a intentarlo.');
          this.loading.set(false);
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }
  load() {
    this.retry.update((value) => value + 1);
  }
  planRoute(pei: Pei) {
    const path = ['/peis', pei.id];
    return this.officialOnly() ? [...path, 'versiones', pei.versionId] : path;
  }
  period(pei: Pei) {
    return `${pei.fechaInicio.slice(0, 4)} — ${pei.fechaFin.slice(0, 4)}`;
  }
  resetFilters() {
    this.query.set('');
    this.type.set('todos');
    this.state.set('todos');
  }
}
