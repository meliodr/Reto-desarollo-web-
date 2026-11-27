import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface RutaObjetivo {
  peiId: string;
  ejeId: string;
  objetivoId: string;
}

export const sentidosIndicador = {
  ascendente: 'Ascendente',
  descendente: 'Descendente',
} as const;

export const metodosIndicador = {
  valor_directo: 'Valor directo',
  variacion_absoluta: 'Variación absoluta',
  variacion_porcentual: 'Variación porcentual',
} as const;

type SentidoIndicador = keyof typeof sentidosIndicador;
type MetodoIndicador = keyof typeof metodosIndicador;

export interface IndicadorWrite {
  nombre: string;
  formula: string;
  unidad: string;
  lineaBase: string | null;
  meta: string;
  frecuencia: string;
  sentido: SentidoIndicador;
  metodoCalculo: MetodoIndicador;
}

export interface Indicador extends Pick<IndicadorWrite, 'nombre'> {
  formula: string | null;
  unidad: string | null;
  lineaBase: string | null;
  meta: string | null;
  frecuencia: string | null;
  sentido: SentidoIndicador | null;
  metodoCalculo: MetodoIndicador | null;
  listoParaMedir: boolean;
  revision: number;
  id: string;
}

export interface MedicionWrite {
  periodo: string;
  valor: string;
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

  detail(ruta: RutaObjetivo, indicadorId: string) {
    return this.http.get<Indicador>(this.detalleUrl(ruta, indicadorId), { observe: 'response' });
  }

  update(ruta: RutaObjetivo, indicadorId: string, tag: string, value: IndicadorWrite) {
    return this.http.put<Indicador>(this.detalleUrl(ruta, indicadorId), value, {
      headers: { 'If-Match': tag },
    });
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

  private detalleUrl(ruta: RutaObjetivo, indicadorId: string) {
    return `${this.url(ruta)}/${encodeURIComponent(indicadorId)}`;
  }

  private medicionesUrl(ruta: RutaObjetivo, indicadorId: string) {
    return `${this.detalleUrl(ruta, indicadorId)}/mediciones`;
  }
}
