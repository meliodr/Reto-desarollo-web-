import { Component, inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router, RouterLink, Routes } from '@angular/router';
import { Session } from './auth/session';

export interface PendingChanges {
  canLeave(): boolean;
}

@Component({
  imports: [RouterLink],
  template: `
    <section class="panel empty-state">
      <h1>{{ session.active()?.nombre }}</h1>
      @if (session.pei() || session.poa()) {
        <a class="button button-primary" [routerLink]="session.home()">Ver planes</a>
      } @else {
        <p>Esta institución tiene los módulos de planificación deshabilitados.</p>
      }
    </section>
  `,
})
class InstitutionHome {
  readonly session = inject(Session);
}

const pendingChangesGuard: CanDeactivateFn<PendingChanges> = (component) => component.canLeave();
const allow =
  (check: (session: Session) => boolean): CanActivateFn =>
  () => {
    const session = inject(Session);
    return check(session) || inject(Router).parseUrl(session.home());
  };

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: () => inject(Session).home() },
  { path: 'institucion', title: 'Institución · SISPLAN', component: InstitutionHome },
  {
    path: 'peis',
    canActivate: [allow((session) => session.pei())],
    title: 'Planes estratégicos · SISPLAN',
    loadComponent: () => import('./pei/pei-list').then((m) => m.PeiList),
  },
  {
    path: 'peis/nuevo',
    canActivate: [allow((session) => session.canPlan())],
    title: 'Nuevo PEI · SISPLAN',
    loadComponent: () => import('./pei/pei-form').then((m) => m.PeiForm),
    canDeactivate: [pendingChangesGuard],
  },
  {
    path: 'peis/:id',
    canActivate: [allow((session) => session.pei())],
    title: 'Plan estratégico · SISPLAN',
    loadComponent: () => import('./pei/pei-form').then((m) => m.PeiForm),
    canDeactivate: [pendingChangesGuard],
  },
  {
    path: 'poas',
    canActivate: [allow((session) => session.poa())],
    title: 'Planes operativos · SISPLAN',
    loadComponent: () => import('./poa/poa-list').then((m) => m.PoaList),
    canDeactivate: [pendingChangesGuard],
  },
  {
    path: 'auditoria',
    canActivate: [allow((session) => session.canAudit())],
    title: 'Historial de auditoría · SISPLAN',
    loadComponent: () => import('./auth/auditoria').then((m) => m.Auditoria),
  },
  {
    path: '**',
    title: 'Página no encontrada · SISPLAN',
    loadComponent: () => import('./pei/not-found').then((m) => m.NotFound),
  },
];
