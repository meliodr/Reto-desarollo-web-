import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { activateAnalyst } from '../../testing/membresia';
import { Pei, PeiApi } from './pei-api';
import { PeiList } from './pei-list';

const official: Pei = {
  id: 'plan-2170',
  versionId: 'version-oficial',
  nombre: 'Plan Estratégico de Atención',
  tipo: 'ordinario',
  fechaInicio: '2170-06-01',
  fechaFin: '2173-03-15',
  mision: 'Servir a la comunidad.',
  vision: 'Una institución accesible.',
  valores: 'Transparencia.',
  estado: 'publicado',
};
const draft: Pei = {
  ...official,
  id: 'plan-borrador',
  versionId: 'version-borrador',
  nombre: 'Borrador de Atención',
  tipo: 'extraordinario',
  fechaInicio: '2170-01-01',
  fechaFin: '2174-12-31',
  estado: 'borrador',
};

describe('búsqueda de PEI', () => {
  let fixture: ComponentFixture<PeiList>;
  let list: PeiList;
  const api = { list: vi.fn() };

  beforeEach(() => {
    api.list.mockImplementation((officialOnly: boolean) =>
      of(officialOnly ? [official] : [official, draft]),
    );
    TestBed.configureTestingModule({
      imports: [PeiList],
      providers: [provideRouter([]), { provide: PeiApi, useValue: api }],
    });
    activateAnalyst();
    fixture = TestBed.createComponent(PeiList);
    list = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('busca los planes publicados por año de vigencia, incluidos ambos extremos', () => {
    list.state.set('publicado');
    fixture.detectChanges();
    expect(api.list).toHaveBeenLastCalledWith(true);
    for (const year of ['2170', ' 2172 ', '2173']) {
      list.query.set(year);
      expect(list.filtered()).toEqual([official]);
    }
    list.query.set('2174');
    expect(list.filtered()).toEqual([]);
  });

  it('combina texto y vigencia con los filtros activos', () => {
    list.query.set('ATENCIÓN');
    expect(list.filtered()).toEqual([official, draft]);

    list.type.set('extraordinario');
    expect(list.filtered()).toEqual([draft]);
    list.query.set('2172');
    expect(list.filtered()).toEqual([draft]);

    list.state.set('publicado');
    fixture.detectChanges();
    expect(list.filtered()).toEqual([]);
  });
});
