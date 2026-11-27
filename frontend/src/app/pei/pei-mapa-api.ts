import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Indicador, RutaObjetivo } from './pei-indicadores-api';
import { Objetivo } from './pei-objetivos-api';
import { Pei } from './pei-api';

export interface RiesgoWrite {
  nombre: string;
  descripcion: string;
  probabilidad: number;
  impacto: number;
  tratamiento: string | null;
  accionMitigacion: string | null;
  responsable: string | null;
  fechaRevision: string | null;
  evidenciaUrl: string | null;
  cerrado: boolean;
}
export interface Riesgo extends RiesgoWrite {
  id: string;
  objetivoId: string;
  nivel: 'bajo' | 'medio' | 'alto';
  puntuacion: number;
  revision: number;
}
interface ObjetivoVersionado extends Objetivo {
  activo: boolean;
  revision: number;
}
export interface ObjetivoMapa extends ObjetivoVersionado {
  indicadores: Indicador[];
  riesgos: Riesgo[];
}
export interface EjeMapa {
  id: string;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  revision: number;
  objetivos: ObjetivoMapa[];
}
export interface MapaEstrategico {
  peiId: string;
  versionId: string;
  estado: Pei['estado'];
  ejes: EjeMapa[];
}

@Injectable({ providedIn: 'root' })
export class PeiMapaApi {
  private readonly http = inject(HttpClient);

  consultar(peiId: string, versionId?: string) {
    return this.http.get<MapaEstrategico>(`/api/v1/peis/${encodeURIComponent(peiId)}/mapa`, {
      params: versionId ? { versionId } : {},
    });
  }

  riesgo(ruta: RutaObjetivo, riesgoId: string) {
    return this.http.get<Riesgo>(this.riesgoUrl(ruta, riesgoId), { observe: 'response' });
  }

  guardarRiesgo(
    ruta: RutaObjetivo,
    datos: RiesgoWrite,
    existente: { id: string; etag: string } | null,
  ) {
    return existente
      ? this.http.put<Riesgo>(this.riesgoUrl(ruta, existente.id), datos, {
          headers: { 'If-Match': existente.etag },
        })
      : this.http.post<Riesgo>(`${this.objetivoUrl(ruta)}/riesgos`, datos);
  }

  objetivo(ruta: RutaObjetivo) {
    return this.http.get<ObjetivoVersionado>(this.objetivoUrl(ruta), { observe: 'response' });
  }

  cambiarEstado(ruta: RutaObjetivo, activo: boolean, etag: string) {
    return this.http.patch<ObjetivoVersionado>(
      `${this.objetivoUrl(ruta)}/estado`,
      { activo },
      { headers: { 'If-Match': etag } },
    );
  }

  private objetivoUrl({ peiId, ejeId, objetivoId }: RutaObjetivo) {
    return (
      `/api/v1/peis/${encodeURIComponent(peiId)}/ejes/${encodeURIComponent(ejeId)}` +
      `/objetivos/${encodeURIComponent(objetivoId)}`
    );
  }

  private riesgoUrl(ruta: RutaObjetivo, riesgoId: string) {
    return `${this.objetivoUrl(ruta)}/riesgos/${encodeURIComponent(riesgoId)}`;
  }
}
