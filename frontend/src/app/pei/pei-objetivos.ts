import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PeiIndicadores } from './pei-indicadores';
import { Eje } from './pei-ejes-api';
import { Objetivo, ObjetivoWrite, PeiObjetivosApi } from './pei-objetivos-api';
import { trimmedMinLength, trimmedRequired } from './pei-validation';

@Component({
  selector: 'app-pei-objetivos',
  imports: [ReactiveFormsModule, PeiIndicadores],
  templateUrl: './pei-objetivos.html',
  styleUrl: './pei-objetivos.css',
})
export class PeiObjetivos {
  private readonly api = inject(PeiObjetivosApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly peiId = input.required<string>();
  readonly eje = input.required<Eje>();
  readonly readOnly = input(false);
  readonly revision = input(0);
  readonly structureChange = output<void>();
  readonly selectedObjective = signal<string | null>(null);
  private readonly indicadores = viewChild(PeiIndicadores);
  private consulta?: Subscription;
  readonly objetivos = signal<Objetivo[]>([]);
  readonly loading = signal(false);
  readonly loadingEdit = signal(false);
  readonly loadError = signal('');
  readonly editing = signal(false);
  readonly editTarget = signal<{ id: string; tag: string } | null>(null);
  readonly saving = signal(false);
  readonly uncertain = signal(false);
  readonly saveError = signal('');
  readonly notice = signal('');
  readonly fields = [
    { key: 'codigo', label: 'Código', max: 50 },
    { key: 'nombre', label: 'Nombre', max: 200 },
    { key: 'descripcion', label: 'Descripción', max: 4000 },
    { key: 'resultadoEsperado', label: 'Resultado esperado', max: 4000 },
    { key: 'lineaAccion', label: 'Línea de acción', max: 4000 },
  ] as const;
  readonly form = inject(NonNullableFormBuilder).group({
    codigo: ['', [trimmedRequired, Validators.maxLength(50)]],
    nombre: ['', [trimmedRequired, trimmedMinLength(3), Validators.maxLength(200)]],
    descripcion: ['', [trimmedRequired, Validators.maxLength(4000)]],
    resultadoEsperado: ['', [trimmedRequired, Validators.maxLength(4000)]],
    lineaAccion: ['', [trimmedRequired, Validators.maxLength(4000)]],
  });

  constructor() {
    effect(() => {
      this.revision();
      untracked(() => this.load());
    });
  }

  hasPendingChanges() {
    return this.saving() || this.form.dirty || !!this.indicadores()?.hasPendingChanges();
  }

  hasPendingWrite() {
    return this.saving() || !!this.indicadores()?.saving();
  }

  load() {
    this.consulta?.unsubscribe();
    this.loading.set(true);
    this.loadError.set('');
    this.consulta = this.api
      .list(this.peiId(), this.eje().id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (objetivos) => {
          this.objetivos.set(objetivos);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set('No pudimos consultar los objetivos de este eje.');
          this.loading.set(false);
        },
      });
  }

  toggleIndicators(id: string) {
    if (this.indicadores()?.hasPendingChanges()) {
      window.alert('Guarda o cancela los cambios de indicadores antes de cambiar de objetivo.');
      return;
    }
    this.selectedObjective.update((selected) => (selected === id ? null : id));
  }

  startCreate() {
    if (!this.canStart()) return;
    this.notice.set('');
    this.saveError.set('');
    this.editing.set(true);
  }

  canStart() {
    return (
      !this.readOnly() &&
      this.eje().activo &&
      !this.editing() &&
      !this.loading() &&
      !this.loadingEdit() &&
      !this.loadError() &&
      !this.indicadores()?.hasPendingChanges()
    );
  }

  startEdit(objetivo: Objetivo) {
    if (!this.canStart() || objetivo.activo === false) return;
    this.notice.set('');
    this.saveError.set('');
    this.loadingEdit.set(true);
    this.api
      .get(this.peiId(), this.eje().id, objetivo.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.loadingEdit.set(false);
          const actual = response.body;
          const tag = response.headers.get('ETag');
          if (!actual || !tag || actual.activo === false) {
            this.saveError.set(
              'Vuelve a consultar el objetivo para obtener una revisión editable.',
            );
            return;
          }
          this.editTarget.set({ id: actual.id, tag });
          const { codigo, nombre, descripcion, resultadoEsperado, lineaAccion } = actual;
          this.form.reset({ codigo, nombre, descripcion, resultadoEsperado, lineaAccion });
          this.editing.set(true);
        },
        error: () => {
          this.loadingEdit.set(false);
          this.saveError.set('No pudimos abrir el objetivo. Vuelve a intentarlo.');
        },
      });
  }

  cancel() {
    const reload = this.uncertain();
    this.resetForm();
    if (reload) this.load();
  }

  fieldError(field: (typeof this.fields)[number]) {
    const control = this.form.controls[field.key];
    if (!control.touched || control.valid) return '';
    if (control.hasError('required')) return 'Completa este campo.';
    if (control.hasError('minlength')) return 'Escribe al menos 3 caracteres.';
    return `Usa un máximo de ${field.max} caracteres.`;
  }

  save() {
    if (
      this.readOnly() ||
      !this.eje().activo ||
      this.loading() ||
      this.loadError() ||
      this.saving() ||
      this.uncertain() ||
      this.targetInactive()
    )
      return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const fields = this.form.getRawValue();
    const value: ObjetivoWrite = {
      codigo: fields.codigo.trim(),
      nombre: fields.nombre.trim(),
      descripcion: fields.descripcion.trim(),
      resultadoEsperado: fields.resultadoEsperado.trim(),
      lineaAccion: fields.lineaAccion.trim(),
    };
    this.saving.set(true);
    this.saveError.set('');
    const target = this.editTarget();
    const request = target
      ? this.api.update(this.peiId(), this.eje().id, target.id, target.tag, value)
      : this.api.create(this.peiId(), this.eje().id, value);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.objetivos.update((items) =>
          target ? items.map((item) => (item.id === saved.id ? saved : item)) : [...items, saved],
        );
        this.structureChange.emit();
        this.resetForm();
        this.notice.set(`Objetivo «${saved.nombre}» guardado.`);
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        if (error.status === 412 || error.status === 428) {
          this.uncertain.set(true);
          this.saveError.set(
            'La revisión cambió. Vuelve a consultar el objetivo antes de guardar.',
          );
        } else if (error.status === 0 || error.status >= 500) {
          this.uncertain.set(true);
          this.saveError.set(
            'No pudimos confirmar el guardado. Consulta los objetivos antes de volver a enviar.',
          );
        } else {
          this.saveError.set(
            error.error?.detail ??
              (error.status === 409
                ? 'Comprueba que el código sea único en este eje y que el eje siga activo.'
                : 'No pudimos guardar el objetivo. Revisa los datos y vuelve a intentarlo.'),
          );
        }
      },
    });
  }

  targetInactive() {
    const target = this.editTarget();
    return !!target && this.objetivos().find((item) => item.id === target.id)?.activo === false;
  }

  private resetForm() {
    this.editing.set(false);
    this.editTarget.set(null);
    this.uncertain.set(false);
    this.saveError.set('');
    this.form.reset();
  }
}
