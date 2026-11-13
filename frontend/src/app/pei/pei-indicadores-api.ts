import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface RutaObjetivo {
  peiId: string;
  ejeId: string;
  objetivoId: string;
}

export interface IndicadorWrite {
  nombre: string;
  formula: string;
  unidad: string;
  lineaBase: number;
  meta: number;
  frecuencia: string;
}

export interface Indicador extends IndicadorWrite {
  id: string;
}

export interface MedicionWrite {
  periodo: string;
  valor: number;
}

export interface Medicion extends MedicionWrite {
  id: string;
}

@Injectable({ providedIn: 'root' })
export class PeiIndicadoresApi {
  private readonly http = inject(HttpClient);

  list(ruta: RutaObjetivo) {
    return this.http.get<Indicador[]>(this.url(ruta));
  }

  create(ruta: RutaObjetivo, value: IndicadorWrite) {
    return this.http.post<Indicador>(this.url(ruta), value);
  }

  listMediciones(ruta: RutaObjetivo, indicadorId: string) {
    return this.http.get<Medicion[]>(this.medicionesUrl(ruta, indicadorId));
  }

  createMedicion(ruta: RutaObjetivo, indicadorId: string, value: MedicionWrite) {
    return this.http.post<Medicion>(this.medicionesUrl(ruta, indicadorId), value);
  }

  private url({ peiId, ejeId, objetivoId }: RutaObjetivo) {
    return (
      `/api/v1/peis/${encodeURIComponent(peiId)}/ejes/${encodeURIComponent(ejeId)}` +
      `/objetivos/${encodeURIComponent(objetivoId)}/indicadores`
    );
  }

  private medicionesUrl(ruta: RutaObjetivo, indicadorId: string) {
    return `${this.url(ruta)}/${encodeURIComponent(indicadorId)}/mediciones`;
  }
}
