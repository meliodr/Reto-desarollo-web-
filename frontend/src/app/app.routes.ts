import { CanDeactivateFn, Routes } from '@angular/router';
export interface PendingChanges {
  canLeave(): boolean;
}
const pendingChangesGuard: CanDeactivateFn<PendingChanges> = (component) => component.canLeave();
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'peis' },
  {
    path: 'peis',
    title: 'Planes estratégicos · SISPLAN',
    loadComponent: () => import('./pei/pei-list').then((m) => m.PeiList),
  },
  {
    path: 'peis/nuevo',
    title: 'Nuevo PEI · SISPLAN',
    loadComponent: () => import('./pei/pei-form').then((m) => m.PeiForm),
    canDeactivate: [pendingChangesGuard],
  },
  {
    path: 'peis/:id',
    title: 'Plan estratégico · SISPLAN',
    loadComponent: () => import('./pei/pei-form').then((m) => m.PeiForm),
    canDeactivate: [pendingChangesGuard],
  },
  {
    path: '**',
    title: 'Página no encontrada · SISPLAN',
    loadComponent: () => import('./pei/not-found').then((m) => m.NotFound),
  },
];
