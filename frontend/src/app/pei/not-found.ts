import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({
  imports: [RouterLink],
  template: `<section class="empty-state panel">
    <span class="eyebrow">PÁGINA NO DISPONIBLE</span>
    <h1>No encontramos esta página.</h1>
    <p>Comprueba la dirección o vuelve al listado de planes estratégicos.</p>
    <a class="button button-primary" routerLink="/peis">Volver a los planes</a>
  </section>`,
})
export class NotFound {}
