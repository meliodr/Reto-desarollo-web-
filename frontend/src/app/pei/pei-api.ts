import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
export interface PeiWrite {
  nombre: string;
  tipo: 'ordinario' | 'extraordinario';
  fechaInicio: string;
  fechaFin: string;
  mision: string;
  vision: string;
  valores: string;
}
export interface Pei extends PeiWrite {
  id: string;
  versionId: string;
  estado: 'borrador';
}
export interface VersionedPei {
  data: Pei;
  etag: string | null;
}
export type PeiField = keyof PeiWrite;
export const fieldLabels: Record<PeiField, string> = {
  nombre: 'Nombre del plan',
  tipo: 'Tipo de PEI',
  fechaInicio: 'Fecha de inicio',
  fechaFin: 'Fecha de fin',
  mision: 'Misión',
  vision: 'Visión',
  valores: 'Valores',
};
export const fieldOrder: PeiField[] = [
  'nombre',
  'tipo',
  'fechaInicio',
  'fechaFin',
  'mision',
  'vision',
  'valores',
];
export function writePayload(value: PeiWrite): PeiWrite {
  return {
    nombre: value.nombre.trim(),
    tipo: value.tipo,
    fechaInicio: value.fechaInicio,
    fechaFin: value.fechaFin,
    mision: value.mision.trim(),
    vision: value.vision.trim(),
    valores: value.valores.trim(),
  };
}
@Injectable({ providedIn: 'root' })
export class PeiApi {
  private readonly http = inject(HttpClient);
  list() {
    return this.http.get<Pei[]>('/api/v1/peis');
  }
  get(id: string) {
    return this.http
      .get<Pei>(`/api/v1/peis/${encodeURIComponent(id)}`, { observe: 'response' })
      .pipe(map((response) => ({ data: response.body!, etag: response.headers.get('ETag') })));
  }
  create(value: PeiWrite) {
    return this.http
      .post<Pei>('/api/v1/peis', writePayload(value), { observe: 'response' })
      .pipe(map((response) => ({ data: response.body!, etag: response.headers.get('ETag') })));
  }
  update(versionId: string, value: PeiWrite, etag: string) {
    return this.http
      .patch<Pei>(`/api/v1/pei-versiones/${encodeURIComponent(versionId)}`, writePayload(value), {
        headers: new HttpHeaders({ 'If-Match': etag }),
        observe: 'response',
      })
      .pipe(map((response) => ({ data: response.body!, etag: response.headers.get('ETag') })));
  }
}
