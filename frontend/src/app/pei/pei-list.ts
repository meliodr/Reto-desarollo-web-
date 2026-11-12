import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Pei, PeiApi } from './pei-api';
@Component({ imports: [RouterLink], templateUrl: './pei-list.html' })
export class PeiList {
  private readonly api = inject(PeiApi);
  readonly peis = signal<Pei[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly query = signal('');
  readonly type = signal('todos');
  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('es');
    return this.peis().filter(
      (p) =>
        (!query ||
          `${p.nombre} ${p.fechaInicio} ${p.fechaFin}`.toLocaleLowerCase('es').includes(query)) &&
        (this.type() === 'todos' || p.tipo === this.type()),
    );
  });
  constructor() {
    this.load();
  }
  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.list().subscribe({
      next: (data) => {
        this.peis.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(
          'No pudimos consultar los planes. Comprueba que la API local esté disponible e inténtalo de nuevo.',
        );
        this.loading.set(false);
      },
    });
  }
  period(pei: Pei) {
    return `${pei.fechaInicio.slice(0, 4)} — ${pei.fechaFin.slice(0, 4)}`;
  }
  resetFilters() {
    this.query.set('');
    this.type.set('todos');
  }
}
