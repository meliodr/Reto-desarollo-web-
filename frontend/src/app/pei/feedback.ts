import { Injectable, signal } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class Feedback {
  readonly message = signal('');
}
