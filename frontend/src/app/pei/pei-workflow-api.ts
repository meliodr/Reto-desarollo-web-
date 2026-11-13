import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { Pei, VersionedPei } from './pei-api';

export type PeiAction = 'enviar' | 'aprobar' | 'devolver' | 'publicar';

export interface PoaPeriod {
  id: string;
  cuatrimestre: number;
  fechaInicio: string;
  fechaFin: string;
  estado: string;
}

export interface PeiPoa {
  id: string;
  anio: number;
  estado: string;
  fechaInicio: string;
  fechaFin: string;
  vigente: boolean;
  evaluaciones: PoaPeriod[];
}

@Injectable({ providedIn: 'root' })
export class PeiWorkflowApi {
  private readonly http = inject(HttpClient);

  actions(versionId: string) {
    return this.http.get<{ acciones: PeiAction[]; motivoDevolucion?: string | null }>(
      this.transitionsUrl(versionId),
    );
  }

  transition(versionId: string, accion: PeiAction, etag: string, motivo?: string) {
    return this.http
      .post<Pei>(
        this.transitionsUrl(versionId),
        { accion, motivo },
        {
          headers: { 'If-Match': etag },
          observe: 'response',
        },
      )
      .pipe(
        map((response): VersionedPei => ({
          data: response.body!,
          etag: response.headers.get('ETag'),
        })),
      );
  }

  poas(peiId: string) {
    return this.http.get<PeiPoa[]>(`/api/v1/peis/${encodeURIComponent(peiId)}/poas`);
  }

  private transitionsUrl(versionId: string) {
    return `/api/v1/pei-versiones/${encodeURIComponent(versionId)}/transiciones`;
  }
}
