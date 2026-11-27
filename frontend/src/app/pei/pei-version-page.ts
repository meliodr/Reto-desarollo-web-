import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { Pei, peiStateLabels } from './pei-api';
import { PeiHistorial } from './pei-versiones';
import { PeiVersion, PeiWorkflowApi } from './pei-workflow-api';
import { PeiMapa } from './pei-mapa';

@Component({
  imports: [DatePipe, RouterLink, PeiHistorial, PeiMapa],
  templateUrl: './pei-version-page.html',
  styles: `
    .narrative {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
    .version-links {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem 1rem;
      margin-block: 0.75rem;
    }
  `,
})
export class PeiVersionPage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PeiWorkflowApi);
  private readonly params = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap,
  });
  private readonly retry = signal(0);
  readonly peiId = computed(() => this.params().get('id')!);
  readonly versionId = computed(() => this.params().get('versionId')!);
  readonly plan = signal<Pei | null>(null);
  readonly versions = signal<PeiVersion[]>([]);
  readonly version = computed(() =>
    this.versions().find((version) => version.versionId === this.versionId()),
  );
  readonly nextVersion = computed(() =>
    this.versions().find((version) => version.versionOrigenId === this.versionId()),
  );
  readonly loading = signal(true);
  readonly error = signal('');
  readonly stateLabel = computed(() => {
    const plan = this.plan();
    if (!plan) return '';
    if (plan.estado !== 'publicado') return peiStateLabels[plan.estado];
    const official = this.versions().find((version) => version.estado === 'publicado');
    if (!official) return 'Versión publicada';
    return official.versionId === plan.versionId ? 'Versión oficial' : 'Publicación histórica';
  });

  constructor() {
    effect((cleanup) => {
      const peiId = this.peiId();
      const versionId = this.versionId();
      this.retry();
      this.loading.set(true);
      this.plan.set(null);
      this.versions.set([]);
      this.error.set('');
      const request = forkJoin({
        plan: this.api.version(versionId),
        versions: this.api.versions(peiId).pipe(catchError(() => of([]))),
      }).subscribe({
        next: ({ plan: { data }, versions }) => {
          this.loading.set(false);
          if (data.id !== peiId || data.versionId !== versionId) {
            this.error.set('La versión solicitada no está disponible para este plan.');
            return;
          }
          this.versions.set(versions);
          this.plan.set(data);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(
            error.status === 404
              ? 'La versión solicitada no está disponible.'
              : 'No pudimos consultar la versión del PEI.',
          );
        },
      });
      cleanup(() => request.unsubscribe());
    });
  }

  load() {
    this.retry.update((value) => value + 1);
  }
}
