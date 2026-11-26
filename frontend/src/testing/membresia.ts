import { TestBed } from '@angular/core/testing';
import { Session } from '../app/auth/session';

export function activateAnalyst(pei = true, poa = true) {
  TestBed.inject(Session).active.set({
    id: '00000000-0000-0000-0000-000000000001',
    nombre: 'Institución A',
    rol: 'AnalistaPlanificacion',
    dependenciaId: null,
    modulos: { pei, poa },
  });
}
