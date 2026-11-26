import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Pei, peiStateLabels } from './pei-api';
import { PeiHistorial } from './pei-versiones';
import { PeiWorkflowApi } from './pei-workflow-api';
import { PeiMapa } from './pei-mapa';

@Component({
  imports: [RouterLink, PeiHistorial, PeiMapa],
  templateUrl: './pei-version-page.html',
  styles: `
    .narrative {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
  `,
})
export class PeiVersionPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PeiWorkflowApi);
  private readonly destroy = inject(DestroyRef);
  readonly peiId = this.route.snapshot.paramMap.get('id')!;
  readonly versionId = this.route.snapshot.paramMap.get('versionId')!;
  readonly plan = signal<Pei | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly stateLabels = peiStateLabels;

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.plan.set(null);
    this.error.set('');
    this.api
      .version(this.versionId)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: ({ data }) => {
          this.loading.set(false);
          if (data.id === this.peiId) this.plan.set(data);
          else this.error.set('La versión solicitada no está disponible para este plan.');
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
  }
}
