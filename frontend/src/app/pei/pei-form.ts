import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Pei, PeiApi, PeiField, PeiWrite, VersionedPei, fieldLabels, fieldOrder } from './pei-api';
import { peiPeriodValidator, trimmedMinLength, trimmedRequired } from './pei-validation';
import { Feedback } from './feedback';

@Component({ imports: [RouterLink, ReactiveFormsModule], templateUrl: './pei-form.html' })
export class PeiForm implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(PeiApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly feedback = inject(Feedback);
  @ViewChild('errorSummary') errorSummary?: ElementRef<HTMLElement>;
  readonly form = this.fb.group(
    {
      nombre: ['', [trimmedRequired, trimmedMinLength(3), Validators.maxLength(200)]],
      tipo: this.fb.control<PeiWrite['tipo']>('ordinario'),
      fechaInicio: ['', Validators.required],
      fechaFin: ['', Validators.required],
      mision: ['', [trimmedRequired, Validators.maxLength(4000)]],
      vision: ['', [trimmedRequired, Validators.maxLength(4000)]],
      valores: ['', [trimmedRequired, Validators.maxLength(4000)]],
    },
    { validators: peiPeriodValidator },
  );
  readonly current = signal<Pei | null>(null);
  readonly etag = signal<string | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal('');
  readonly saving = signal(false);
  readonly submitted = signal(false);
  readonly saveError = signal('');
  readonly serverErrors = signal<Record<string, string[]>>({});
  readonly conflict = signal(false);
  readonly latest = signal<VersionedPei | null>(null);
  readonly checkingLatest = signal(false);
  readonly requestUncertain = signal(false);
  readonly saved = signal(false);
  readonly labels = fieldLabels;
  readonly narrativeFields: PeiField[] = ['mision', 'vision', 'valores'];
  private id: string | null = null;
  ngOnInit() {
    this.id = this.route.snapshot.paramMap.get('id');
    if (this.id) this.load();
  }
  load() {
    if (!this.id) return;
    this.loading.set(true);
    this.loadError.set('');
    this.api.get(this.id).subscribe({
      next: (value) => {
        this.apply(value);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loadError.set(
          error.status === 404
            ? 'Este PEI no existe o ya no está disponible.'
            : 'No pudimos abrir el PEI. Comprueba la API local y vuelve a intentarlo.',
        );
        this.loading.set(false);
      },
    });
  }
  private apply(value: VersionedPei) {
    this.current.set(value.data);
    this.etag.set(value.etag);
    const p = value.data;
    this.form.reset({
      nombre: p.nombre,
      tipo: p.tipo,
      fechaInicio: p.fechaInicio,
      fechaFin: p.fechaFin,
      mision: p.mision,
      vision: p.vision,
      valores: p.valores,
    });
    this.submitted.set(false);
    this.serverErrors.set({});
    this.saveError.set('');
    this.conflict.set(false);
    this.latest.set(null);
    this.requestUncertain.set(false);
    if (!value.etag)
      this.saveError.set(
        'La API no devolvió la versión del registro (ETag). Vuelve a consultar el PEI antes de editarlo.',
      );
  }
  isNew() {
    return !this.id;
  }
  years() {
    return this.form.controls.tipo.value === 'ordinario' ? 4 : 5;
  }
  periodLabel() {
    const { fechaInicio, fechaFin } = this.form.getRawValue();
    return fechaInicio && fechaFin
      ? `${fechaInicio.slice(0, 4)} — ${fechaFin.slice(0, 4)}`
      : 'Período por definir';
  }
  fieldError(field: PeiField): string {
    const remote = this.serverErrors()[field];
    if (remote?.length) return remote.join(' ');
    const control = this.form.controls[field];
    if (!this.submitted() && !control.touched) return '';
    if (control.hasError('required'))
      return `Completa ${field === 'nombre' ? 'el nombre del plan' : field === 'mision' ? 'la misión' : field === 'vision' ? 'la visión' : field === 'valores' ? 'los valores' : field === 'fechaInicio' ? 'la fecha de inicio' : 'la fecha de fin'}.`;
    if (control.hasError('minlength'))
      return 'Escribe al menos 3 caracteres para el nombre del plan.';
    if (control.hasError('maxlength'))
      return `Usa un máximo de ${field === 'nombre' ? 200 : 4000} caracteres.`;
    if (field === 'fechaFin') {
      if (this.form.hasError('invalidDates')) return 'Introduce fechas de calendario válidas.';
      if (this.form.hasError('reversedDates'))
        return 'La fecha de fin debe ser igual o posterior a la de inicio.';
      if (this.form.hasError('periodYears'))
        return `El período debe abarcar ${this.years()} años calendario, contando el año inicial y el final.`;
    }
    return '';
  }
  errors() {
    return fieldOrder
      .map((field) => ({ field, message: this.fieldError(field) }))
      .filter((error) => error.message);
  }
  clearFieldError(field: PeiField) {
    const errors = { ...this.serverErrors() };
    delete errors[field];
    this.serverErrors.set(errors);
    this.saved.set(false);
  }
  focusField(event: Event, field: PeiField) {
    event.preventDefault();
    document.getElementById(field)?.focus();
  }
  private focusErrors() {
    setTimeout(() => this.errorSummary?.nativeElement.focus());
  }
  submit() {
    if (this.saving() || this.requestUncertain()) return;
    this.submitted.set(true);
    this.saveError.set('');
    this.serverErrors.set({});
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.focusErrors();
      return;
    }
    const existing = this.current(),
      etag = this.etag();
    if (existing && !etag) {
      this.saveError.set(
        'Falta la versión del registro. Vuelve a abrir este PEI antes de guardar.',
      );
      this.focusErrors();
      return;
    }
    this.saving.set(true);
    this.saved.set(false);
    const request = existing
      ? this.api.update(existing.versionId, this.form.getRawValue(), etag!)
      : this.api.create(this.form.getRawValue());
    request.subscribe({
      next: (value) => {
        this.saving.set(false);
        this.form.markAsPristine();
        this.feedback.message.set(
          `El borrador «${value.data.nombre}» está disponible en el listado.`,
        );
        if (!existing) {
          this.router.navigate(['/peis', value.data.id]);
        } else {
          this.apply(value);
          this.saved.set(true);
        }
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        if (error.status === 422) {
          const errors = error.error?.errors;
          if (errors && typeof errors === 'object') {
            const mapped: Record<string, string[]> = {};
            for (const [key, value] of Object.entries(errors)) {
              const normalized = key.replace(/^\$\./, '').replace(/^./, (c) => c.toLowerCase());
              mapped[normalized] = Array.isArray(value) ? value.map(String) : [String(value)];
            }
            this.serverErrors.set(mapped);
          }
          this.saveError.set('Revisa los datos señalados. La API no ha guardado estos cambios.');
        } else if (error.status === 412) {
          this.conflict.set(true);
          this.saveError.set(
            'Otra sesión cambió este PEI. Tus cambios siguen aquí: consulta la versión actual antes de continuar.',
          );
        } else if (error.status === 428) {
          this.saveError.set(
            'La API necesita una versión válida para guardar. Vuelve a consultar este PEI.',
          );
        } else if (error.status === 0 || error.status >= 500) {
          this.requestUncertain.set(true);
          this.saveError.set(
            'No pudimos confirmar el resultado del guardado. Antes de volver a enviar, consulta el listado o la versión actual para comprobar si se guardó. Tus datos siguen en este formulario.',
          );
        } else {
          this.saveError.set(
            error.status === 403
              ? 'No tienes permiso para guardar este registro.'
              : error.status === 409
                ? 'El estado del PEI cambió y ya no permite esta operación. Consulta la versión actual.'
                : 'No fue posible guardar el borrador. Tus datos permanecen en el formulario.',
          );
        }
        this.focusErrors();
      },
    });
  }
  fetchLatest() {
    if (!this.id) return;
    this.checkingLatest.set(true);
    this.api.get(this.id).subscribe({
      next: (value) => {
        this.latest.set(value);
        this.checkingLatest.set(false);
      },
      error: () => {
        this.saveError.set(
          'No pudimos consultar la versión actual. Tus cambios se conservan; inténtalo de nuevo.',
        );
        this.checkingLatest.set(false);
      },
    });
  }
  replaceWithLatest() {
    const latest = this.latest();
    if (
      latest &&
      window.confirm(
        'Se reemplazarán los campos de este formulario por la versión guardada en el servidor. Tus cambios sin guardar se descartarán. ¿Continuar?',
      )
    )
      this.apply(latest);
  }
  differences() {
    const latest = this.latest();
    if (!latest) return [];
    const local = this.form.getRawValue();
    return fieldOrder
      .filter((field) => local[field] !== latest.data[field])
      .map((field) => ({
        field,
        label: fieldLabels[field],
        local: local[field],
        server: latest.data[field],
      }));
  }
  acknowledgeUncertain() {
    if (
      window.confirm(
        'Confirma que ya revisaste el listado o el registro y que estos cambios no se guardaron. Volver a enviar podría crear un duplicado si la primera solicitud sí terminó.',
      )
    ) {
      this.requestUncertain.set(false);
      this.saveError.set('');
    }
  }
  canLeave() {
    return (
      !this.form.dirty ||
      window.confirm('Tienes cambios sin guardar. ¿Quieres salir y descartarlos?')
    );
  }
  @HostListener('window:beforeunload', ['$event']) beforeUnload(event: BeforeUnloadEvent) {
    if (this.form.dirty) {
      event.preventDefault();
      event.returnValue = '';
    }
  }
}
