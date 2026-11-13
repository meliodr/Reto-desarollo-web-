import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Session } from '../auth/session';
import { Pei, VersionedPei } from './pei-api';
import { PeiAction, PeiWorkflowApi } from './pei-workflow-api';

const actionLabels: Record<PeiAction, string> = {
  enviar: 'Enviar a revisión',
  aprobar: 'Aprobar PEI',
  devolver: 'Devolver a borrador',
  publicar: 'Publicar PEI',
};

const successMessages: Record<PeiAction, string> = {
  enviar: 'PEI enviado a revisión.',
  aprobar: 'PEI aprobado.',
  devolver: 'PEI devuelto a borrador.',
  publicar: 'PEI publicado. Sus POA anuales están disponibles.',
};

@Component({
  selector: 'app-pei-workflow',
  templateUrl: './pei-workflow.html',
  styleUrl: './pei-workflow.css',
})
export class PeiWorkflow {
  private readonly api = inject(PeiWorkflowApi);
  private readonly session = inject(Session);
  private readonly destroyRef = inject(DestroyRef);
  readonly pei = input.required<Pei>();
  readonly etag = input.required<string | null>();
  readonly dirty = input(false);
  readonly changed = output<VersionedPei>();
  readonly pendingChange = output<boolean>();
  readonly refresh = output<void>();
  readonly loading = signal(true);
  readonly pending = signal(false);
  readonly actions = signal<PeiAction[]>([]);
  readonly actionsError = signal('');
  readonly transitionError = signal('');
  readonly requiresRefresh = signal(false);
  readonly notice = signal('');
  readonly confirmAction = signal<PeiAction | null>(null);
  readonly reason = signal('');
  readonly returnReason = signal<string | null>(null);
  readonly actionLabels = actionLabels;

  constructor() {
    effect((cleanup) => {
      const plan = this.pei();
      this.returnReason.set(null);
      if (!this.session.canPlan() || plan.estado === 'publicado') {
        this.actions.set([]);
        this.loading.set(false);
        return;
      }
      this.loading.set(true);
      this.actions.set([]);
      this.actionsError.set('');
      const request = this.api.actions(plan.versionId).subscribe({
        next: (response) => {
          this.actions.set(response.acciones);
          this.returnReason.set(response.motivoDevolucion ?? null);
          this.loading.set(false);
        },
        error: () => {
          this.actionsError.set('No pudimos consultar las acciones del PEI.');
          this.loading.set(false);
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }

  select(action: PeiAction) {
    if (this.dirty() || this.pending() || !this.actions().includes(action)) return;
    if (action === 'devolver' || action === 'publicar') {
      this.reason.set('');
      this.confirmAction.set(action);
    } else {
      this.transition(action);
    }
  }

  transition(action: PeiAction) {
    const etag = this.etag();
    if (
      !this.session.canPlan() ||
      !etag ||
      this.dirty() ||
      this.pending() ||
      this.requiresRefresh() ||
      !this.actions().includes(action)
    )
      return;
    const reason = this.reason().trim();
    if (action === 'devolver' && (!reason || reason.length > 4000)) return;

    this.pending.set(true);
    this.pendingChange.emit(true);
    this.notice.set('');
    this.transitionError.set('');
    this.api
      .transition(this.pei().versionId, action, etag, action === 'devolver' ? reason : undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          this.changed.emit(value);
          this.confirmAction.set(null);
          this.reason.set('');
          this.notice.set(successMessages[action]);
          this.finish();
        },
        error: (error: HttpErrorResponse) => {
          this.requiresRefresh.set(
            error.status === 412 ||
              error.status === 428 ||
              error.status === 0 ||
              error.status >= 500,
          );
          this.transitionError.set(
            error.status === 412 || error.status === 428
              ? 'El PEI cambió en otra sesión. Actualiza el registro antes de intentarlo de nuevo.'
              : error.status === 0 || error.status >= 500
                ? 'El resultado quedó sin confirmar. Actualiza el PEI para consultar su estado.'
                : error.error?.detail ||
                  error.error?.title ||
                  'No se pudo actualizar el estado del PEI.',
          );
          this.finish();
        },
      });
  }

  private finish() {
    this.pending.set(false);
    this.pendingChange.emit(false);
  }
}
