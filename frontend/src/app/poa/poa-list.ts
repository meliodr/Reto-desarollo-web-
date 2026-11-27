import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Session } from '../auth/session';
import { Pei } from '../pei/pei-api';

interface Dependencia {
  id: string;
  nombre: string;
  activa: boolean;
}

interface Poa {
  id: string;
  anio: number;
  estado: string;
  fechaInicio: string;
  fechaFin: string;
  peiId: string | null;
  peiVersionId: string | null;
  peiNombre: string | null;
  presupuestoReferencia: string | null;
  dependenciaId: string | null;
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
  readonly yearError = computed(() => {
    const year = this.year();
    if (year === null) return 'Introduce el año del POA.';
    if (!Number.isInteger(year) || year < 1 || year > 9999)
      return 'Introduce un año entero entre 1 y 9999.';
    return '';
  });
  readonly peiId = signal('');
  readonly budget = signal('');
  readonly dependenciaElegida = signal('');
  readonly dependencias = signal<Dependencia[]>([]);
  readonly dependenciasActivas = computed(() => this.dependencias().filter((area) => area.activa));
  readonly nombresDependencias = computed(
    () => new Map(this.dependencias().map((area) => [area.id, area.nombre])),
  );
  readonly dependenciaAsignada = computed(() => this.session.active()?.dependenciaId ?? null);
  readonly cargandoDependencias = signal(false);
  readonly errorDependencias = signal(false);
  readonly candidates = computed(() => {
    const year = this.year();
    if (this.yearError()) return this.strategic().filter((plan) => plan.id === this.peiId());
    return this.strategic().filter(
      (plan) =>
        year !== null &&
        Number(plan.fechaInicio.slice(0, 4)) <= year &&
        year <= Number(plan.fechaFin.slice(0, 4)),
    );
  });

  constructor() {
    this.load();
    this.cargarDependencias();
  }

  cargarDependencias() {
    this.cargandoDependencias.set(true);
    this.errorDependencias.set(false);
    this.http
      .get<{ dependencias: Dependencia[] }>('/api/v1/organizacion')
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.cargandoDependencias.set(false)),
      )
      .subscribe({
        next: ({ dependencias }) => this.dependencias.set(dependencias),
        error: () => this.errorDependencias.set(true),
      });
  }

  cerrarFormulario() {
    this.showForm.set(false);
    this.error.set('');
    this.budget.set('');
    this.dependenciaElegida.set('');
    this.peiId.set('');
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
      .get<Pei[]>('/api/v1/peis', { params: { oficiales: true } })
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (peis) => this.strategic.set(peis.filter((plan) => plan.estado === 'publicado')),
        error: () => this.peiLoadError.set(true),
      });
  }

  comprobarVigenciaPei() {
    const id = this.peiId();
    if (id && !this.yearError() && !this.candidates().some((plan) => plan.id === id)) {
      this.peiId.set('');
    }
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
      this.cargandoDependencias() ||
      this.yearError()
    )
      return;
    this.error.set('');
    this.notice.set('');
    const budget = this.budget().trim().replace(',', '.');
    if (budget && !/^[0-9]{1,12}(\.[0-9]{1,2})?$/.test(budget)) {
      this.error.set('Indica un presupuesto con hasta 12 dígitos enteros y 2 decimales.');
      return;
    }
    this.comprobarVigenciaPei();
    this.saving.set(true);
    this.http
      .post<Poa>('/api/v1/poas', {
        anio,
        peiId: (this.session.pei() && this.peiId()) || null,
        presupuestoReferencia: budget || null,
        dependenciaId: this.dependenciaAsignada() ?? (this.dependenciaElegida() || null),
      })
      .pipe(
        takeUntilDestroyed(this.destroy),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (poa) => {
          this.plans.update((plans) => [...plans, poa].sort((a, b) => b.anio - a.anio));
          this.notice.set(`POA ${poa.anio} creado.`);
          this.cerrarFormulario();
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
