import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';

export interface EjeWrite {
  nombre: string;
  descripcion: string | null;
}

export interface Eje extends EjeWrite {
  id: string;
  activo: boolean;
  revision: number;
  actualizadoEn: string;
}

@Injectable({ providedIn: 'root' })
export class PeiEjesApi {
  private readonly http = inject(HttpClient);

  list(peiId: string) {
    return this.http.get<Eje[]>(this.url(peiId));
  }

  get(peiId: string, ejeId: string) {
    return this.http
      .get<Eje>(this.url(peiId, ejeId), { observe: 'response' })
      .pipe(map((response) => ({ eje: response.body!, etag: response.headers.get('ETag') })));
  }

  create(peiId: string, value: EjeWrite) {
    return this.http.post<Eje>(this.url(peiId), value);
  }

  update(peiId: string, eje: Eje, value: EjeWrite, activo: boolean, etag: string) {
    return this.http.put<Eje>(
      this.url(peiId, eje.id),
      { ...value, activo, revision: eje.revision },
      { headers: { 'If-Match': etag } },
    );
  }

  history(peiId: string, ejeId: string) {
    return this.http.get<Eje[]>(`${this.url(peiId, ejeId)}/versiones`);
  }

  private url(peiId: string, ejeId?: string) {
    const listUrl = `/api/v1/peis/${encodeURIComponent(peiId)}/ejes`;
    return ejeId ? `${listUrl}/${encodeURIComponent(ejeId)}` : listUrl;
  }
}
