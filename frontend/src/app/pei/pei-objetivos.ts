import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Eje } from './pei-ejes-api';
import { Objetivo, ObjetivoWrite, PeiObjetivosApi } from './pei-objetivos-api';
import { trimmedMinLength, trimmedRequired } from './pei-validation';

@Component({
  selector: 'app-pei-objetivos',
  imports: [ReactiveFormsModule],
  templateUrl: './pei-objetivos.html',
  styleUrl: './pei-objetivos.css',
})
export class PeiObjetivos implements OnInit {
  private readonly api = inject(PeiObjetivosApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly peiId = input.required<string>();
  readonly eje = input.required<Eje>();
  readonly objetivos = signal<Objetivo[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal('');
  readonly editing = signal(false);
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

  ngOnInit() {
    this.load();
  }

  hasPendingChanges() {
    return this.saving() || this.form.dirty;
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');
    this.api
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

  startCreate() {
    this.notice.set('');
    this.editing.set(true);
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
    if (!this.eje().activo || this.saving() || this.uncertain()) return;
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
    this.api
      .create(this.peiId(), this.eje().id, value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.objetivos.update((items) => [...items, saved]);
          this.resetForm();
          this.notice.set(`Objetivo «${saved.nombre}» guardado.`);
        },
        error: (error: HttpErrorResponse) => {
          this.saving.set(false);
          if (error.status === 0 || error.status >= 500) {
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

  private resetForm() {
    this.editing.set(false);
    this.uncertain.set(false);
    this.saveError.set('');
    this.form.reset();
  }
}
