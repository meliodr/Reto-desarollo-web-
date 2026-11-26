import { HttpClient } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { Session } from './auth/session';
import { Pei, PeiApi, peiStateLabels } from './pei/pei-api';

interface PoaSummary { id: string; anio: number; estado: string; }
type Result<T> = { data: T[]; failed: boolean };

@Component({
  imports: [RouterLink],
  template: `
    <section class="page-heading">
      <div><div class="eyebrow">Mi espacio de trabajo</div>
      <h1>Tu planificación.</h1><p>{{ session.active()?.nombre }} · Consulta el estado de tus planes institucionales.</p></div>
      @if (session.canPlan()) {
        <a class="button button-primary" routerLink="/peis/nuevo"><span class="button-plus" aria-hidden="true">+</span> Crear nuevo PEI</a>
      }
    </section>
    @if (!session.pei() && !session.poa()) {
      <section class="panel empty-state"><h2>Módulos no habilitados</h2><p>Esta institución tiene los módulos de planificación deshabilitados.</p></section>
    } @else {
      <div class="flow-bento">
        @if (session.pei()) {
          <a class="flow-tile cobalt" routerLink="/peis"><span>Planes estratégicos</span><strong>{{ peiLoading() ? '…' : peiFailed() ? '—' : peis().length }}</strong><span>Consultar PEI →</span></a>
        }
        @if (session.poa()) {
          <a class="flow-tile indigo" routerLink="/poas"><span>Planes operativos</span><strong>{{ poaLoading() ? '…' : poaFailed() ? '—' : poas().length }}</strong><span>Consultar POA →</span></a>
        }
        @if (session.pei()) {
          <a class="flow-tile sky" routerLink="/peis" [queryParams]="{estado:'publicado'}"><span>PEI publicados</span>
            <div class="flow-ring"><svg viewBox="0 0 100 100" aria-hidden="true"><circle cx="50" cy="50" r="39" fill="none" stroke="currentColor" opacity=".20" stroke-width="10"/><circle cx="50" cy="50" r="39" fill="none" stroke="currentColor" stroke-width="10" stroke-linecap="round" pathLength="100" [attr.stroke-dasharray]="publicationRate() + ' 100'" transform="rotate(-90 50 50)"/></svg><strong>{{ peiLoading() ? '…' : peiFailed() || !peis().length ? '—' : publicationRate() + '%' }}</strong></div>
            <span>Publicación, no cumplimiento →</span></a>
        }
      </div>
      @if (peiFailed() || poaFailed()) {
        <p class="notice notice-error" role="alert">No fue posible consultar todos los planes. <button class="button button-secondary" (click)="reload()">Reintentar</button></p>
      }
      <div class="flow-dashboard-grid">
        @if (session.pei()) {
          <section class="panel flow-dashboard-panel"><div class="section-heading"><h2>PEI por revisar</h2><a routerLink="/peis" class="row-action">Ver planes →</a></div>
            @if (peiLoading()) { <p role="status" class="muted">Consultando planes…</p> }
            @else if (peiFailed()) { <p class="muted">Información no disponible.</p> }
            @else {
              @for (p of review(); track p.id) {
                <a class="flow-record" [routerLink]="['/peis',p.id]"><span>{{ p.nombre }}<small>{{ labels[p.estado] }}</small></span><span aria-hidden="true">›</span></a>
              } @empty { <p class="muted">No hay PEI en revisión u observados.</p> }
            }
          </section>
          <section class="panel flow-dashboard-panel"><div class="section-heading"><h2>Estado de publicación</h2></div>
            @if (peiLoading() || peiFailed()) { <p class="muted">{{ peiLoading() ? 'Consultando…' : 'Información no disponible.' }}</p> }
            @else {
              @for (state of states(); track state.key) {
                <a class="flow-chart-row" routerLink="/peis" [queryParams]="{estado: state.key}" [title]="state.label + ': ' + state.count">
                  <span>{{ state.label }} <b>{{ state.count }}</b></span><span class="flow-track"><i [style.width.%]="state.percent" [style.background]="state.color"></i></span>
                </a>
              }
            }
            <p class="muted">Distribución real de estados. El avance mensual requiere las mediciones de seguimiento.</p>
          </section>
        }
        @if (session.poa()) {
          <section class="panel flow-dashboard-panel"><div class="section-heading"><h2>POA registrados</h2><a routerLink="/poas" class="row-action">Ver POA →</a></div>
            @if (poaLoading()) { <p class="muted" role="status">Consultando POA…</p> }
            @else if (poaFailed()) { <p class="muted">Información no disponible.</p> }
            @else { @for (p of poas().slice(0,5); track p.id) { <a class="flow-record" routerLink="/poas"><span>POA {{ p.anio }}<small>{{ p.estado }}</small></span><span>›</span></a> } @empty { <p class="muted">Todavía no hay POA registrados.</p> } }
          </section>
        }
      </div>
    }
  `,
})
export class InstitutionHome {
  readonly session = inject(Session);
  private readonly api = inject(PeiApi);
  private readonly http = inject(HttpClient);
  readonly retry = signal(0);
  readonly peis = signal<Pei[]>([]);
  readonly poas = signal<PoaSummary[]>([]);
  readonly peiLoading = signal(false);
  readonly poaLoading = signal(false);
  readonly peiFailed = signal(false);
  readonly poaFailed = signal(false);
  readonly labels = peiStateLabels;
  readonly review = computed(() => this.peis().filter(p => p.estado === 'en_revision' || p.estado === 'observado').slice(0, 5));
  readonly publicationRate = computed(() => this.peis().length ? Math.round(100 * this.peis().filter(p => p.estado === 'publicado').length / this.peis().length) : 0);
  readonly states = computed(() => {
    const colors = ['#64748b','#2563eb','#9333ea','#6366f1','#0891b2','#312e81'];
    return (Object.keys(peiStateLabels) as (keyof typeof peiStateLabels)[]).map((key,i) => {
      const count = this.peis().filter(p=>p.estado===key).length;
      return {key,label:peiStateLabels[key],count,percent:this.peis().length?100*count/this.peis().length:0,color:colors[i]};
    });
  });
  reload() { this.retry.update(v=>v+1); }
  constructor() {
    effect(cleanup => {
      const active = this.session.active();
      this.retry();
      this.peis.set([]); this.poas.set([]);
      this.peiFailed.set(false); this.poaFailed.set(false);
      this.peiLoading.set(!!active?.modulos.pei); this.poaLoading.set(!!active?.modulos.poa);
      if (!active) return;
      const pei = active.modulos.pei ? this.api.list().pipe(map(data => ({data,failed:false})),catchError(() => of({data:[] as Pei[],failed:true}))).subscribe((res:Result<Pei>) => {this.peis.set(res.data);this.peiFailed.set(res.failed);this.peiLoading.set(false);}) : null;
      const poa = active.modulos.poa ? this.http.get<PoaSummary[]>('/api/v1/poas').pipe(map(data=>({data,failed:false})),catchError(()=>of({data:[] as PoaSummary[],failed:true}))).subscribe((res:Result<PoaSummary>)=>{this.poas.set(res.data);this.poaFailed.set(res.failed);this.poaLoading.set(false);}) : null;
      cleanup(()=>{pei?.unsubscribe();poa?.unsubscribe();});
    });
  }
}
