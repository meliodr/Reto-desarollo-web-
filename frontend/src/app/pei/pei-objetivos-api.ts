import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface ObjetivoWrite {
  codigo: string;
  nombre: string;
  descripcion: string;
  resultadoEsperado: string;
  lineaAccion: string;
}

export interface Objetivo extends ObjetivoWrite {
  id: string;
}

@Injectable({ providedIn: 'root' })
export class PeiObjetivosApi {
  private readonly http = inject(HttpClient);

  list(peiId: string, ejeId: string) {
    return this.http.get<Objetivo[]>(this.url(peiId, ejeId));
  }

  create(peiId: string, ejeId: string, value: ObjetivoWrite) {
    return this.http.post<Objetivo>(this.url(peiId, ejeId), value);
  }

  private url(peiId: string, ejeId: string) {
    return `/api/v1/peis/${encodeURIComponent(peiId)}/ejes/${encodeURIComponent(ejeId)}/objetivos`;
  }
}
