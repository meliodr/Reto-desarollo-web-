import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Session } from '../auth/session';
import { Pei } from '../pei/pei-api';

interface Poa {
  id: string;
  anio: number;
  estado: string;
  fechaInicio: string;
  fechaFin: string;
  peiId: string | null;
  peiNombre: string | null;
  presupuestoReferencia: string | null;
}

@Component({
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './poa-list.html',
  styles: [
    '.poa-form {max-width: 680px; margin-bottom: 1.5rem} .poa-form button {margin-top: 1rem}',
  ],
})
export class PoaList {
  private readonly http = inject(HttpClient);
  private readonly destroy = inject(DestroyRef);
  readonly session = inject(Session);
  readonly plans = signal<Poa[]>([]);
  readonly strategic = signal<Pei[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly peiLoadError = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly showForm = signal(false);
  readonly year = signal<number | null>(new Date().getFullYear());
  readonly peiId = signal('');
  readonly budget = signal('');
  readonly candidates = computed(() => {
    const year = this.year();
    return this.strategic().filter(
      (plan) =>
        year !== null &&
        Number(plan.fechaInicio.slice(0, 4)) <= year &&
        year <= Number(plan.fechaFin.slice(0, 4)),
    );
  });

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.loadError.set(false);
    this.http
      .get<Poa[]>('/api/v1/poas')
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (plans) => this.plans.set(plans),
        error: () => this.loadError.set(true),
      });

    if (!this.session.pei()) return;
    this.peiLoadError.set(false);
    this.http
      .get<Pei[]>('/api/v1/peis')
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (peis) => this.strategic.set(peis.filter((plan) => plan.estado === 'publicado')),
        error: () => this.peiLoadError.set(true),
      });
  }

  canLeave() {
    if (this.saving()) return false;
    return !this.showForm() || window.confirm('¿Salir y descartar el formulario del POA?');
  }

  save() {
    const anio = this.year();
    if (
      !this.session.canFormulatePoa() ||
      this.saving() ||
      anio === null ||
      !Number.isInteger(anio) ||
      anio < 1 ||
      anio > 9999
    )
      return;
    this.error.set('');
    this.notice.set('');
    const budget = this.budget().trim().replace(',', '.');
    if (budget && !/^[0-9]{1,12}(\.[0-9]{1,2})?$/.test(budget)) {
      this.error.set('Indica un presupuesto con hasta 12 dígitos enteros y 2 decimales.');
      return;
    }
    this.saving.set(true);
    this.http
      .post<Poa>('/api/v1/poas', {
        anio,
        peiId: (this.session.pei() && this.peiId()) || null,
        presupuestoReferencia: budget || null,
      })
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (poa) => {
          this.plans.update((plans) => [...plans, poa].sort((a, b) => b.anio - a.anio));
          this.notice.set(`POA ${poa.anio} creado.`);
          this.budget.set('');
          this.showForm.set(false);
        },
        error: (error: HttpErrorResponse) =>
          this.error.set(
            error.error?.errors?.presupuestoReferencia?.[0] ??
              error.error?.title ??
              'No pudimos guardar el POA.',
          ),
      });
  }
}
