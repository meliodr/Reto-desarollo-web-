import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Indicador,
  IndicadorWrite,
  Medicion,
  PeiIndicadoresApi,
  RutaObjetivo,
} from './pei-indicadores-api';
import { trimmedMinLength, trimmedRequired } from './pei-validation';

const decimalPattern = /^-?\d{1,12}(?:[.,]\d{1,4})?$/;
const decimalValidators = [Validators.required, Validators.pattern(decimalPattern)];
const maxText = (limit: number) => [trimmedRequired, Validators.maxLength(limit)];

@Component({
  selector: 'app-pei-indicadores',
  imports: [DecimalPipe, ReactiveFormsModule],
  templateUrl: './pei-indicadores.html',
  styleUrl: './pei-indicadores.css',
})
export class PeiIndicadores implements OnInit {
  private readonly api = inject(PeiIndicadoresApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly builder = inject(FormBuilder).nonNullable;
  readonly ruta = input.required<RutaObjetivo>();
  readonly readOnly = input(false);
  readonly indicadores = signal<Indicador[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly editing = signal(false);
  readonly selected = signal<Indicador | null>(null);
  readonly mediciones = signal<Medicion[]>([]);
  readonly loadingMediciones = signal(false);
  readonly medicionesError = signal(false);
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly uncertain = signal(false);
  readonly notice = signal('');
  readonly form = this.builder.group({
    nombre: ['', [...maxText(200), trimmedMinLength(3)]],
    formula: ['', maxText(4000)],
    unidad: ['', maxText(100)],
    lineaBase: ['', decimalValidators],
    meta: ['', decimalValidators],
    frecuencia: ['', maxText(100)],
  });
  readonly medicionForm = this.builder.group({
    periodo: ['', Validators.required],
    valor: ['', decimalValidators],
  });
  readonly formId = computed(() => `indicadores-${this.ruta().objetivoId}`);

  ngOnInit() {
    this.load();
  }

  hasPendingChanges() {
    return this.saving() || this.form.dirty || this.medicionForm.dirty;
  }

  load() {
    this.loading.set(true);
    this.loadError.set(false);
    this.api
      .list(this.ruta())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (indicadores) => {
          this.indicadores.set(indicadores);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  startCreate() {
    if (this.readOnly() || !this.closeMeasurements()) return;
    this.notice.set('');
    this.editing.set(true);
  }

  cancelIndicator() {
    const reload = this.uncertain();
    this.form.reset();
    this.editing.set(false);
    this.clearError();
    if (reload) this.load();
  }

  select(indicador: Indicador) {
    const wasOpen = this.selected()?.id === indicador.id;
    if (!this.closeMeasurements() || wasOpen) return;
    this.notice.set('');
    this.selected.set(indicador);
    this.loadMediciones(indicador);
  }

  loadMediciones(indicador: Indicador) {
    this.loadingMediciones.set(true);
    this.medicionesError.set(false);
    this.api
      .listMediciones(this.ruta(), indicador.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (mediciones) => {
          if (this.selected()?.id !== indicador.id) return;
          this.mediciones.set(mediciones);
          this.loadingMediciones.set(false);
        },
        error: () => {
          if (this.selected()?.id !== indicador.id) return;
          this.medicionesError.set(true);
          this.loadingMediciones.set(false);
        },
      });
  }

  saveIndicator() {
    this.form.markAllAsTouched();
    if (this.readOnly() || this.form.invalid || this.saving() || this.uncertain()) return;
    const raw = this.form.getRawValue();
    const value: IndicadorWrite = {
      nombre: raw.nombre.trim(),
      formula: raw.formula.trim(),
      unidad: raw.unidad.trim(),
      lineaBase: Number(raw.lineaBase.replace(',', '.')),
      meta: Number(raw.meta.replace(',', '.')),
      frecuencia: raw.frecuencia.trim(),
    };
    this.saving.set(true);
    this.clearError();
    this.api
      .create(this.ruta(), value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.indicadores.update((items) => [...items, saved]);
          this.cancelIndicator();
          this.notice.set(`Indicador «${saved.nombre}» guardado.`);
        },
        error: (error: HttpErrorResponse) => this.handleSaveError(error),
      });
  }

  saveMeasurement(indicador: Indicador) {
    this.medicionForm.markAllAsTouched();
    if (this.readOnly() || this.medicionForm.invalid || this.saving() || this.uncertain()) return;
    const raw = this.medicionForm.getRawValue();
    this.saving.set(true);
    this.clearError();
    this.api
      .createMedicion(this.ruta(), indicador.id, {
        periodo: raw.periodo,
        valor: Number(raw.valor.replace(',', '.')),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.medicionForm.reset();
          this.notice.set('Medición guardada.');
          this.loadMediciones(indicador);
        },
        error: (error: HttpErrorResponse) => this.handleSaveError(error),
      });
  }

  resetMeasurement(indicador: Indicador) {
    const reload = this.uncertain();
    this.medicionForm.reset();
    this.clearError();
    if (reload) this.loadMediciones(indicador);
  }

  private closeMeasurements() {
    if (this.saving() || this.editing()) return false;
    if (this.medicionForm.dirty && !window.confirm('¿Descartar la medición sin guardar?'))
      return false;
    this.selected.set(null);
    this.medicionForm.reset();
    this.mediciones.set([]);
    this.clearError();
    return true;
  }

  private clearError() {
    this.saveError.set('');
    this.uncertain.set(false);
  }

  private handleSaveError(error: HttpErrorResponse) {
    this.saving.set(false);
    if (error.status === 0 || error.status >= 500) {
      this.uncertain.set(true);
      this.saveError.set(
        'No pudimos confirmar el guardado. Consulta el listado antes de volver a enviar.',
      );
      return;
    }
    const fields = error.error?.errors as Record<string, string[]> | undefined;
    this.saveError.set(
      (fields && Object.values(fields).flat().join(' ')) ||
        error.error?.detail ||
        error.error?.title ||
        'No pudimos guardar los datos. Revisa los campos y vuelve a intentarlo.',
    );
  }
}
