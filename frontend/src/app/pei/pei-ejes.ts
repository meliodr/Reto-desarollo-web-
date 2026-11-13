import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, ViewChild, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Eje, EjeWrite, PeiEjesApi } from './pei-ejes-api';
import { trimmedMinLength, trimmedRequired } from './pei-validation';
import { PeiObjetivos } from './pei-objetivos';

@Component({
  selector: 'app-pei-ejes',
  imports: [DatePipe, ReactiveFormsModule, PeiObjetivos],
  templateUrl: './pei-ejes.html',
  styleUrl: './pei-ejes.css',
})
export class PeiEjes implements OnInit {
  private readonly api = inject(PeiEjesApi);
  private readonly destroyRef = inject(DestroyRef);
  readonly peiId = input.required<string>();
  readonly ejes = signal<Eje[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal('');
  readonly editLoading = signal(false);
  readonly editor = signal<'create' | 'edit' | null>(null);
  readonly editing = signal<Eje | null>(null);
  readonly form = inject(NonNullableFormBuilder).group({
    nombre: ['', [trimmedRequired, trimmedMinLength(3), Validators.maxLength(200)]],
    descripcion: ['', Validators.maxLength(4000)],
  });
  readonly confirmation = signal(false);
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly blocked = signal(false);
  readonly notice = signal('');
  readonly history = signal<Eje | null>(null);
  readonly revisions = signal<Eje[]>([]);
  readonly loadingHistory = signal(false);
  readonly historyError = signal('');
  readonly objetivosEjeId = signal<string | null>(null);
  @ViewChild(PeiObjetivos) private objetivos?: PeiObjetivos;
  private etag: string | null = null;

  ngOnInit() {
    this.load();
  }

  hasPendingChanges() {
    return this.saving() || this.form.dirty || !!this.objetivos?.hasPendingChanges();
  }

  load() {
    this.loading.set(true);
    this.loadError.set('');
    this.api
      .list(this.peiId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ejes) => {
          this.ejes.set(ejes);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set('No pudimos consultar los ejes. Vuelve a intentarlo.');
          this.loading.set(false);
        },
      });
  }

  startCreate() {
    this.resetEditor();
    this.editor.set('create');
  }

  startEdit(eje: Eje) {
    this.resetEditor();
    this.editLoading.set(true);
    this.api
      .get(this.peiId(), eje.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ eje: latest, etag }) => {
          this.editLoading.set(false);
          this.etag = etag;
          this.updateList(latest);
          if (!latest.activo || !etag) {
            this.saveError.set(
              latest.activo
                ? 'No pudimos identificar la revisión de este eje. Vuelve a abrirlo.'
                : 'El eje ya está inactivo. Su historial sigue disponible.',
            );
            return;
          }
          this.editing.set(latest);
          this.form.reset({ nombre: latest.nombre, descripcion: latest.descripcion ?? '' });
          this.editor.set('edit');
        },
        error: () => {
          this.editLoading.set(false);
          this.saveError.set('No pudimos abrir el eje. Vuelve a intentarlo.');
        },
      });
  }

  cancel() {
    if (this.blocked()) this.load();
    this.resetEditor();
  }

  save() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const form = this.form.getRawValue();
    this.submit({ nombre: form.nombre.trim(), descripcion: form.descripcion.trim() || null }, true);
  }

  inactivate() {
    const eje = this.editing();
    if (eje) this.submit({ nombre: eje.nombre, descripcion: eje.descripcion }, false);
  }

  showHistory(eje: Eje) {
    this.history.set(eje);
    this.revisions.set([]);
    this.historyError.set('');
    this.loadingHistory.set(true);
    this.api
      .history(this.peiId(), eje.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (revisions) => {
          this.revisions.set(revisions);
          this.loadingHistory.set(false);
        },
        error: () => {
          this.historyError.set('No pudimos consultar las versiones del eje. Vuelve a intentarlo.');
          this.loadingHistory.set(false);
        },
      });
  }

  toggleObjetivos(ejeId: string) {
    if (this.objetivos?.saving()) return;
    if (
      this.objetivos?.hasPendingChanges() &&
      !window.confirm('Tienes un objetivo sin guardar. ¿Quieres descartar sus cambios?')
    )
      return;
    this.objetivosEjeId.update((current) => (current === ejeId ? null : ejeId));
  }

  private submit(value: EjeWrite, activo: boolean) {
    if (this.saving() || this.blocked() || !this.editor()) return;
    const eje = this.editing();
    if (eje && !this.etag) return;
    const request = eje
      ? this.api.update(this.peiId(), eje, value, activo, this.etag!)
      : this.api.create(this.peiId(), value);
    this.saving.set(true);
    this.saveError.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.updateList(saved);
        this.resetEditor();
        this.notice.set(
          activo ? `Eje «${saved.nombre}» guardado.` : `Eje «${saved.nombre}» inactivado.`,
        );
        if (this.history()?.id === saved.id) this.showHistory(saved);
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        this.confirmation.set(false);
        if (error.status === 412 || error.status === 409 || error.status === 404) {
          this.blocked.set(true);
          this.saveError.set(
            'El eje o el PEI cambió en otra sesión. Revisa el listado y vuelve a abrir el eje.',
          );
        } else if (error.status === 0 || error.status >= 500) {
          this.blocked.set(true);
          this.saveError.set(
            'No pudimos confirmar el guardado. Consulta el listado antes de volver a enviar.',
          );
        } else {
          this.saveError.set(
            error.error?.detail ??
              'No pudimos guardar el eje. Revisa los datos e inténtalo de nuevo.',
          );
        }
      },
    });
  }

  private updateList(eje: Eje) {
    this.ejes.update((ejes) =>
      ejes.some((item) => item.id === eje.id)
        ? ejes.map((item) => (item.id === eje.id ? eje : item))
        : [...ejes, eje],
    );
  }

  private resetEditor() {
    this.editor.set(null);
    this.editing.set(null);
    this.etag = null;
    this.form.reset({ nombre: '', descripcion: '' });
    this.confirmation.set(false);
    this.blocked.set(false);
    this.notice.set('');
    this.saveError.set('');
  }
}
