import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Session } from '../auth/session';
import { Pei, peiStateLabels } from './pei-api';
import { PeiAction, PeiHistoryEntry, PeiVersion, PeiWorkflowApi } from './pei-workflow-api';

const historyLabels: Record<PeiAction, string> = {
  enviar: 'Envío a revisión',
  revisar: 'Revisión técnica',
  validar_comite: 'Validación del Comité Estratégico',
  devolver: 'Devolución con observaciones',
  corregir: 'Corrección',
  aprobar: 'Aprobación',
  publicar: 'Publicación oficial',
};

@Component({
  selector: 'app-pei-historial',
  imports: [DatePipe],
  template: `
    @if (loading()) {
      <p role="status">Consultando el historial…</p>
    } @else if (error()) {
      <p class="notice notice-error" role="alert">No pudimos consultar el historial.</p>
      <button type="button" class="button button-secondary" (click)="reload()">Reintentar</button>
    } @else if (!entries().length) {
      <p class="muted">Aún no se han registrado acciones de revisión para esta versión.</p>
    } @else {
      <ol class="pei-history">
        @for (entry of entries(); track entry.revision) {
          <li>
            <div class="history-heading">
              <strong>{{ actionLabels[entry.accion] }}</strong>
              <time [attr.datetime]="entry.registradaEn">
                {{ entry.registradaEn | date: 'dd/MM/yyyy, HH:mm' }}
              </time>
            </div>
            <p>
              {{ states[entry.estadoAnterior] }} → {{ states[entry.estadoNuevo] }}
              <span class="muted">· Revisión {{ entry.revision }}</span>
            </p>
            @if (entry.motivo; as reason) {
              <p class="history-reason"><strong>Motivo</strong> {{ reason }}</p>
            }
            <small class="muted identity" [title]="entry.emisor">Actor {{ entry.sujeto }}</small>
          </li>
        }
      </ol>
    }
  `,
  styles: `
    .pei-history {
      list-style: none;
      padding: 0;
      margin: 0;
      display: grid;
      gap: 1rem;
    }
    .pei-history li {
      padding: 0.8rem 1rem;
      border-inline-start: 2px solid currentColor;
    }
    .history-heading {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem 1rem;
      align-items: baseline;
    }
    .history-heading time {
      font-size: 0.85rem;
    }
    .pei-history p {
      margin: 0.35rem 0;
    }
    .history-reason {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
    .identity {
      overflow-wrap: anywhere;
    }
  `,
})
export class PeiHistorial {
  private readonly api = inject(PeiWorkflowApi);
  private readonly retry = signal(0);
  readonly versionId = input.required<string>();
  readonly refreshKey = input<string | null>(null);
  readonly entries = signal<PeiHistoryEntry[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly states = peiStateLabels;
  readonly actionLabels = historyLabels;

  constructor() {
    effect((cleanup) => {
      this.refreshKey();
      this.retry();
      this.loading.set(true);
      this.error.set(false);
      const request = this.api.history(this.versionId()).subscribe({
        next: (entries) => {
          this.entries.set(entries);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }

  reload() {
    this.retry.update((value) => value + 1);
  }
}

@Component({
  selector: 'app-pei-versiones',
  imports: [DatePipe, RouterLink, PeiHistorial],
  templateUrl: './pei-versiones.html',
})
export class PeiVersiones {
  private readonly api = inject(PeiWorkflowApi);
  private readonly destroy = inject(DestroyRef);
  private readonly retry = signal(0);
  readonly session = inject(Session);
  readonly pei = input.required<Pei>();
  readonly refreshKey = input<string | null>(null);
  readonly revisionCreated = output<void>();
  readonly refreshPlan = output<void>();
  readonly creating = output<boolean>();
  readonly open = signal(false);
  readonly versions = signal<PeiVersion[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly states = peiStateLabels;
  readonly editRevision = signal(false);
  readonly saving = signal(false);
  readonly reloadRequired = signal(false);
  readonly reason = signal('');
  readonly revisionError = signal('');

  constructor() {
    effect((cleanup) => {
      if (!this.open()) return;
      this.refreshKey();
      this.retry();
      this.loading.set(true);
      this.error.set(false);
      const request = this.api.versions(this.pei().id).subscribe({
        next: (versions) => {
          this.versions.set(versions);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }

  reload() {
    this.retry.update((value) => value + 1);
  }

  startRevision() {
    this.reason.set('');
    this.revisionError.set('');
    this.editRevision.set(true);
  }

  createRevision() {
    const etag = this.refreshKey(),
      reason = this.reason().trim();
    if (
      !this.session.canPlan() ||
      this.pei().estado !== 'publicado' ||
      !etag ||
      !reason ||
      reason.length > 4000 ||
      this.saving() ||
      this.reloadRequired()
    )
      return;
    this.saving.set(true);
    this.creating.emit(true);
    this.revisionError.set('');
    this.api
      .createRevision(this.pei().id, reason, etag)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: () => {
          this.editRevision.set(false);
          this.saving.set(false);
          this.creating.emit(false);
          this.revisionCreated.emit();
        },
        error: (error: HttpErrorResponse) => {
          this.saving.set(false);
          this.creating.emit(false);
          this.reloadRequired.set([0, 409, 412, 428].includes(error.status) || error.status >= 500);
          this.revisionError.set(
            error.error?.detail ??
              (this.reloadRequired()
                ? 'Actualiza el PEI para consultar la revisión que está disponible.'
                : 'No pudimos crear la revisión. Revisa el motivo e inténtalo de nuevo.'),
          );
        },
      });
  }
}
