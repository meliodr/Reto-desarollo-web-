import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, Subscription, switchMap } from 'rxjs';
import { RutaObjetivo, metodosIndicador, sentidosIndicador } from './pei-indicadores-api';
import {
  EjeMapa,
  MapaEstrategico,
  ObjetivoMapa,
  PeiMapaApi,
  Riesgo,
  RiesgoWrite,
} from './pei-mapa-api';
import { trimmedMinLength, trimmedRequired } from './pei-validation';

type Filtro = 'todos' | 'activos' | 'inactivos' | 'pendientes';

interface EdicionRiesgo {
  ruta: RutaObjetivo;
  existente: { id: string; etag: string } | null;
}

function normalizar(texto: string) {
  return texto
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .trim()
    .toLowerCase();
}

function puntuar(probabilidad: number, impacto: number) {
  return Math.max(0, probabilidad * impacto);
}

function nivel(puntuacion: number) {
  return puntuacion >= 15 ? 'alto' : puntuacion >= 5 ? 'medio' : 'bajo';
}

@Component({
  selector: 'app-pei-mapa',
  imports: [ReactiveFormsModule],
  templateUrl: './pei-mapa.html',
  styleUrl: './pei-mapa.css',
})
export class PeiMapa {
  private readonly api = inject(PeiMapaApi);
  private readonly destroy = inject(DestroyRef);
  private consulta?: Subscription;
  readonly peiId = input.required<string>();
  readonly versionId = input<string>();
  readonly readOnly = input(false);
  readonly refreshKey = input<number | string>(0);
  readonly structureChange = output<void>();

  readonly abierto = signal(false);
  readonly mapa = signal<MapaEstrategico | null>(null);
  readonly cargando = signal(false);
  readonly errorConsulta = signal('');
  readonly texto = signal('');
  readonly filtro = signal<Filtro>('todos');
  readonly filtroEje = signal('');
  readonly aviso = signal('');
  readonly errorCambios = signal('');
  readonly operacion = signal(false);
  readonly edicion = signal<EdicionRiesgo | null>(null);
  readonly pendienteConfirmar = signal<{ eje: EjeMapa; objetivo: ObjetivoMapa } | null>(null);
  readonly errores = signal<Record<string, string[]>>({});
  readonly incierto = signal(false);
  readonly escalas = [1, 2, 3, 4, 5];
  readonly metodos = metodosIndicador;
  readonly sentidos = sentidosIndicador;
  readonly form = inject(NonNullableFormBuilder).group({
    nombre: ['', [trimmedMinLength(3), Validators.maxLength(200)]],
    descripcion: ['', [trimmedRequired, Validators.maxLength(4000)]],
    probabilidad: [1],
    impacto: [1],
    tratamiento: ['', [Validators.maxLength(200)]],
    accionMitigacion: ['', [Validators.maxLength(4000)]],
    responsable: ['', [Validators.maxLength(200)]],
    fechaRevision: [''],
    evidenciaUrl: ['', [Validators.maxLength(2048)]],
    cerrado: [false],
  });
  private readonly valores = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  readonly puntuacion = computed(() => {
    const { probabilidad = 1, impacto = 1 } = this.valores();
    return puntuar(probabilidad, impacto);
  });
  readonly nivel = computed(() => nivel(this.puntuacion()));
  readonly editable = computed(() => {
    const estado = this.mapa()?.estado;
    return (
      !this.readOnly() && !this.versionId() && (estado === 'borrador' || estado === 'observado')
    );
  });
  readonly nodos = computed(() => {
    const ejes = this.mapa()?.ejes ?? [];
    const texto = normalizar(this.texto());
    const estado = this.filtro();
    return ejes
      .filter((eje) => !this.filtroEje() || eje.id === this.filtroEje())
      .map((eje) => {
        const coincideEje = normalizar([eje.nombre, eje.descripcion].join(' ')).includes(texto);
        const objetivos = eje.objetivos.filter((objetivo) => {
          const activo = eje.activo && objetivo.activo;
          const campos = [
            objetivo.codigo,
            objetivo.nombre,
            objetivo.descripcion,
            objetivo.resultadoEsperado,
            objetivo.lineaAccion,
            ...objetivo.indicadores.map((indicador) => indicador.nombre),
            ...objetivo.riesgos.map((riesgo) => riesgo.nombre),
          ];
          const coincideTexto = coincideEje || normalizar(campos.join(' ')).includes(texto);
          return (
            coincideTexto &&
            (estado === 'todos' ||
              (estado === 'activos' && activo) ||
              (estado === 'inactivos' && !activo) ||
              (estado === 'pendientes' &&
                activo &&
                objetivo.indicadores.some((indicador) => !indicador.listoParaMedir)))
          );
        });
        const mostrarEjeVacio =
          !eje.objetivos.length &&
          coincideEje &&
          (estado === 'todos' ||
            (estado === 'activos' && eje.activo) ||
            (estado === 'inactivos' && !eje.activo));
        return { ...eje, objetivos, mostrar: !!objetivos.length || mostrarEjeVacio };
      })
      .filter((eje) => eje.mostrar);
  });

  constructor() {
    effect(() => {
      const peiId = this.peiId();
      const versionId = this.versionId();
      this.refreshKey();
      if (this.abierto()) untracked(() => this.cargar(peiId, versionId));
    });
  }

  hasPendingChanges() {
    return this.operacion() || this.form.dirty || !!this.pendienteConfirmar();
  }

  alternar() {
    if (!this.abierto() || !this.hasPendingChanges()) this.abierto.update((abierto) => !abierto);
  }

  cargar(peiId = this.peiId(), versionId = this.versionId()) {
    this.consulta?.unsubscribe();
    this.cargando.set(true);
    this.errorConsulta.set('');
    this.consulta = this.api
      .consultar(peiId, versionId)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (mapa) => {
          this.mapa.set(mapa);
          this.cargando.set(false);
        },
        error: () => {
          this.errorConsulta.set('No pudimos consultar el mapa estratégico.');
          this.cargando.set(false);
        },
      });
  }

  cambiarFiltro(evento: Event) {
    this.filtro.set((evento.target as HTMLSelectElement).value as Filtro);
  }

  limpiarFiltros() {
    this.texto.set('');
    this.filtro.set('todos');
    this.filtroEje.set('');
  }

  esEditable(eje: EjeMapa, objetivo: ObjetivoMapa) {
    return this.editable() && eje.activo && objetivo.activo;
  }

  prepararRiesgo(eje: EjeMapa, objetivo: ObjetivoMapa, riesgo?: Riesgo) {
    if (
      !this.esEditable(eje, objetivo) ||
      this.edicion() ||
      this.operacion() ||
      this.pendienteConfirmar()
    )
      return;
    const ruta = this.ruta(eje, objetivo);
    this.limpiarMensajes();
    if (!riesgo) {
      this.form.reset();
      this.edicion.set({ ruta, existente: null });
      return;
    }
    this.operacion.set(true);
    this.api
      .riesgo(ruta, riesgo.id)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (response) => {
          this.operacion.set(false);
          const riesgoActual = response.body;
          const etag = response.headers.get('ETag');
          if (!riesgoActual || !etag) {
            this.errorCambios.set('No pudimos obtener la versión actual del riesgo.');
            return;
          }
          this.form.reset({
            ...riesgoActual,
            tratamiento: riesgoActual.tratamiento ?? '',
            accionMitigacion: riesgoActual.accionMitigacion ?? '',
            responsable: riesgoActual.responsable ?? '',
            fechaRevision: riesgoActual.fechaRevision ?? '',
            evidenciaUrl: riesgoActual.evidenciaUrl ?? '',
          });
          this.edicion.set({ ruta, existente: { id: riesgo.id, etag } });
        },
        error: () => {
          this.operacion.set(false);
          this.errorCambios.set('No pudimos consultar el riesgo para editarlo.');
        },
      });
  }

  cancelarRiesgo() {
    if (this.operacion()) return;
    const volverAConsultar = this.incierto() || !!this.errorCambios();
    this.edicion.set(null);
    this.form.reset();
    this.limpiarMensajes();
    if (volverAConsultar) this.cargar();
  }

  guardarRiesgo() {
    const edicion = this.edicion();
    if (!this.editable() || !edicion || this.operacion() || this.incierto()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.limpiarMensajes();
    const campos = this.form.getRawValue();
    const datos: RiesgoWrite = {
      nombre: campos.nombre.trim(),
      descripcion: campos.descripcion.trim(),
      probabilidad: campos.probabilidad,
      impacto: campos.impacto,
      tratamiento: campos.tratamiento.trim() || null,
      accionMitigacion: campos.accionMitigacion.trim() || null,
      responsable: campos.responsable.trim() || null,
      fechaRevision: campos.fechaRevision || null,
      evidenciaUrl: campos.evidenciaUrl.trim() || null,
      cerrado: campos.cerrado,
    };
    if (datos.evidenciaUrl && !this.httpsValido(datos.evidenciaUrl)) {
      this.errores.set({ evidenciaUrl: ['Usa una URL HTTPS sin usuario ni contraseña.'] });
      this.errorCambios.set('Revisa la URL de evidencia.');
      return;
    }
    if (
      datos.cerrado &&
      nivel(puntuar(datos.probabilidad, datos.impacto)) === 'alto' &&
      (!datos.tratamiento ||
        !datos.accionMitigacion ||
        !datos.responsable ||
        !datos.fechaRevision ||
        !datos.evidenciaUrl)
    ) {
      this.errorCambios.set(
        'Para cerrar un riesgo alto, completa el tratamiento, la acción de mitigación, el responsable, la fecha de revisión y la evidencia.',
      );
      return;
    }
    this.operacion.set(true);
    this.api
      .guardarRiesgo(edicion.ruta, datos, edicion.existente)
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe({
        next: (guardado) => {
          this.operacion.set(false);
          this.cancelarRiesgo();
          this.aviso.set(`Riesgo «${guardado.nombre}» guardado.`);
          this.cargar();
        },
        error: (error: HttpErrorResponse) => this.errorEscritura(error),
      });
  }

  cambiarEstado() {
    const confirmacion = this.pendienteConfirmar();
    if (!confirmacion || !this.editable() || this.operacion() || this.edicion()) return;
    const { eje, objetivo } = confirmacion;
    this.pendienteConfirmar.set(null);
    this.operacion.set(true);
    this.limpiarMensajes();
    const ruta = this.ruta(eje, objetivo);
    this.api
      .objetivo(ruta)
      .pipe(
        switchMap((respuesta) => {
          const etag = respuesta.headers.get('ETag');
          if (
            !etag ||
            respuesta.body?.revision !== objetivo.revision ||
            respuesta.body.activo !== objetivo.activo
          ) {
            this.operacion.set(false);
            this.errorCambios.set('El objetivo cambió. Consulta el mapa y revisa su estado.');
            this.cargar();
            return EMPTY;
          }
          return this.api.cambiarEstado(ruta, !objetivo.activo, etag);
        }),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({
        next: () => {
          this.operacion.set(false);
          this.aviso.set(objetivo.activo ? 'Objetivo desactivado.' : 'Objetivo activado.');
          this.structureChange.emit();
          this.cargar();
        },
        error: (error: HttpErrorResponse) => {
          this.errorEscritura(error);
          this.cargar();
        },
      });
  }

  mensajeCampo(campo: keyof typeof this.form.controls) {
    const control = this.form.controls[campo];
    if (control.touched && control.invalid) {
      if (campo === 'nombre' && control.hasError('minlength'))
        return 'Escribe al menos 3 caracteres.';
      if (control.hasError('required')) return 'Completa este campo.';
      return 'El valor excede el máximo permitido.';
    }
    return this.errores()[campo]?.[0] ?? '';
  }

  private ruta(eje: EjeMapa, objetivo: ObjetivoMapa): RutaObjetivo {
    return { peiId: this.peiId(), ejeId: eje.id, objetivoId: objetivo.id };
  }

  private limpiarMensajes() {
    this.aviso.set('');
    this.errorCambios.set('');
    this.errores.set({});
    this.incierto.set(false);
  }

  private errorEscritura(error: HttpErrorResponse) {
    this.operacion.set(false);
    if (error.status === 0 || error.status >= 500 || error.status === 412) {
      this.incierto.set(true);
      this.errorCambios.set(
        error.status === 412
          ? 'Otra persona actualizó el registro. Cancela y consulta la versión actual para editarla.'
          : 'No pudimos confirmar el guardado. Consulta el mapa antes de hacer otro cambio.',
      );
      return;
    }
    this.errores.set(error.error?.errors ?? {});
    this.errorCambios.set(
      error.error?.detail ??
        'No pudimos guardar el cambio. Revisa los campos e inténtalo de nuevo.',
    );
  }

  private httpsValido(value: string) {
    try {
      const url = new URL(value);
      return (
        /^https:\/\//i.test(value) &&
        !/\s/.test(value) &&
        Boolean(url.hostname) &&
        !url.username &&
        !url.password
      );
    } catch {
      return false;
    }
  }
}
