import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router, Routes } from '@angular/router';
import { Session } from './auth/session';
import { InstitutionHome } from './institution-home';

export interface PendingChanges {
  canLeave(): boolean;
}

const pendingChangesGuard: CanDeactivateFn<PendingChanges> = (component) =>
  component?.canLeave() ?? true;
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
    path: 'peis/:id/versiones/:versionId',
    canActivate: [allow((session) => session.pei())],
    title: 'Versión del PEI · SISPLAN',
    loadComponent: () => import('./pei/pei-version-page').then((m) => m.PeiVersionPage),
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
