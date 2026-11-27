import { DatePipe } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

interface EventoAuditoria {
  id: number;
  emisor: string;
  sujeto: string;
  accion: 'INSERT' | 'UPDATE' | 'DELETE';
  entidad: string;
  referencia: string;
  fecha: string;
}

@Component({
  imports: [DatePipe],
  template: `
    <div class="page-heading"><h1>Historial de auditoría</h1></div>
    @if (events().length) {
      <div class="panel table-scroll" role="region" aria-label="Cambios registrados" tabindex="0">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Cuenta</th>
              <th>Acción</th>
              <th>Entidad</th>
              <th>Referencia</th>
            </tr>
          </thead>
          <tbody>
            @for (event of events(); track event.id) {
              <tr>
                <td>{{ event.fecha | date: 'dd/MM/yyyy HH:mm:ss' }}</td>
                <td>
                  {{ event.sujeto }}<small>{{ event.emisor }}</small>
                </td>
                <td>{{ actions[event.accion] }}</td>
                <td>{{ event.entidad }}</td>
                <td>{{ event.referencia }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
    @if (loading()) {
      <p role="status">Consultando historial…</p>
    } @else if (error()) {
      <p class="notice notice-error" role="alert">
        No se pudo consultar el historial de esta institución.
      </p>
      <button class="button button-secondary" type="button" (click)="load()">Reintentar</button>
    } @else if (!events().length) {
      <p>Aún no se han registrado cambios en esta institución.</p>
    } @else if (hasMore()) {
      <button class="button button-secondary" type="button" (click)="load()">
        Ver cambios anteriores
      </button>
    }
  `,
})
export class Auditoria {
  private readonly http = inject(HttpClient);
  private readonly destroy = inject(DestroyRef);
  readonly events = signal<EventoAuditoria[]>([]);
  readonly error = signal(false);
  readonly loading = signal(false);
  readonly hasMore = signal(false);
  readonly actions = { INSERT: 'Creación', UPDATE: 'Actualización', DELETE: 'Eliminación' };

  constructor() {
    this.load();
  }

  load() {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(false);
    const cursor = this.events().at(-1)?.id;
    const params = cursor ? new HttpParams().set('antesDe', cursor) : undefined;
    this.http
      .get<EventoAuditoria[]>('/api/v1/auditoria', { params })
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (page) => {
          this.events.update((events) => [...events, ...page]);
          this.hasMore.set(page.length === 100);
        },
        error: () => this.error.set(true),
      });
  }
}
