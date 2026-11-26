import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Session } from './auth/session';
import { InstitutionHome } from './institution-home';
import { Pei } from './pei/pei-api';
import { activateAnalyst } from '../testing/membresia';

const plan = (id: string, estado: Pei['estado']): Pei => ({
  id, estado, versionId: id + '-v', nombre: 'PEI ' + id,
  tipo:'ordinario', fechaInicio:'2026-01-01', fechaFin:'2029-12-31',
  mision:'Misión', vision:'Visión', valores:'Valores',
});

describe('Inicio institucional Flow', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports:[InstitutionHome],
    providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting()],
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('consulta solo PEI habilitado y calcula los estados con los registros reales', () => {
    activateAnalyst(true,false);
    const fixture = TestBed.createComponent(InstitutionHome);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/v1/peis').flush([plan('1','publicado'),plan('2','observado')]);
    http.expectNone('/api/v1/poas');
    fixture.detectChanges();
    expect(fixture.componentInstance.publicationRate()).toBe(50);
    expect(fixture.componentInstance.review().map(p=>p.id)).toEqual(['2']);
    expect(fixture.nativeElement.textContent).toContain('50%');
    expect(fixture.nativeElement.textContent).toContain('Publicación, no cumplimiento');
  });

  it('consulta solo POA habilitado', () => {
    activateAnalyst(false,true);
    const fixture = TestBed.createComponent(InstitutionHome);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectNone('/api/v1/peis');
    http.expectOne('/api/v1/poas').flush([{id:'poa1',anio:2026,estado:'borrador'}]);
    fixture.detectChanges();
    expect(fixture.componentInstance.poas().length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('POA 2026');
  });

  it('no consulta módulos sin institución activa', () => {
    TestBed.inject(Session).active.set(null);
    const fixture = TestBed.createComponent(InstitutionHome);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectNone('/api/v1/peis');
    http.expectNone('/api/v1/poas');
  });
});
