import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { activateAnalyst } from '../testing/membresia';
import { App } from './app';

@Component({ template: '<h1>Planes operativos anuales</h1>' })
class PoaPage {}

it('el enlace de salto enfoca el contenido y conserva la ruta del módulo', async () => {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [provideRouter([{ path: 'poas', component: PoaPage }])],
  });
  activateAnalyst();
  const fixture = TestBed.createComponent(App);
  const router = TestBed.inject(Router);
  await router.navigateByUrl('/poas?origen=menu');
  fixture.detectChanges();
  const skip: HTMLAnchorElement = fixture.nativeElement.querySelector('.skip-link');
  const main: HTMLElement = fixture.nativeElement.querySelector('#main-content');
  const page = main.querySelector('h1');
  expect(page?.textContent).toBe('Planes operativos anuales');
  skip.focus();
  const activation = new MouseEvent('click', { bubbles: true, cancelable: true, detail: 0 });
  skip.dispatchEvent(activation);
  expect(activation.defaultPrevented).toBe(true);
  expect(document.activeElement).toBe(main);
  expect(router.url).toBe('/poas?origen=menu');
  expect(main.querySelector('h1')).toBe(page);
});
