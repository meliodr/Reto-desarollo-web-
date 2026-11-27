import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { Pei, PeiState, VersionedPei } from './pei-api';

export type PeiAction =
  'enviar' | 'revisar' | 'validar_comite' | 'aprobar' | 'devolver' | 'corregir' | 'publicar';

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

export interface PeiVersion {
  versionId: string;
  numero: number;
  revision: number;
  estado: PeiState;
  fechaInicio: string;
  fechaFin: string;
  actualizadoEn: string;
  publicadoEn: string | null;
  versionOrigenId: string | null;
  motivoRevision: string | null;
  creadaPorEmisor: string | null;
  creadaPorSujeto: string | null;
  creadoEn: string;
}

export interface PeiHistoryEntry {
  revision: number;
  accion: PeiAction;
  estadoAnterior: PeiState;
  estadoNuevo: PeiState;
  emisor: string;
  sujeto: string;
  motivo: string | null;
  registradaEn: string;
}

@Injectable({ providedIn: 'root' })
export class PeiWorkflowApi {
  private readonly http = inject(HttpClient);

  actions(versionId: string) {
    return this.http.get<{
      acciones: PeiAction[];
      motivoDevolucion?: string | null;
      indicadoresDigemapsPendientes?: number;
    }>(this.transitionsUrl(versionId));
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

  versions(peiId: string) {
    return this.http.get<PeiVersion[]>(this.versionsUrl(peiId));
  }

  createRevision(peiId: string, motivo: string, etag: string) {
    return this.http
      .post<Pei>(
        this.versionsUrl(peiId),
        { motivo },
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

  private versionsUrl(peiId: string) {
    return `/api/v1/peis/${encodeURIComponent(peiId)}/versiones`;
  }

  version(versionId: string) {
    return this.http.get<Pei>(this.versionUrl(versionId), { observe: 'response' }).pipe(
      map((response): VersionedPei => ({
        data: response.body!,
        etag: response.headers.get('ETag'),
      })),
    );
  }

  history(versionId: string) {
    return this.http.get<PeiHistoryEntry[]>(this.versionUrl(versionId) + '/historial');
  }

  private versionUrl(versionId: string) {
    return `/api/v1/pei-versiones/${encodeURIComponent(versionId)}`;
  }

  private transitionsUrl(versionId: string) {
    return this.versionUrl(versionId) + '/transiciones';
  }
}
