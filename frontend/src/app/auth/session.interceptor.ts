import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, takeUntil, tap, throwError } from 'rxjs';
import { Session } from './session';

export const sessionInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/v1/')) return next(request);
  const session = inject(Session);
  const institution = session.active();
  if (!institution) {
    return throwError(
      () => new HttpErrorResponse({ status: 403, statusText: 'Sin institución activa' }),
    );
  }
  return from(session.accessToken()).pipe(
    switchMap((token) =>
      next(
        request.clone({
          setHeaders: {
            Authorization: 'Bearer ' + token,
            'X-Institucion-Id': institution.id,
          },
        }),
      ),
    ),
    tap({
      error: (error) => {
        if (error instanceof HttpErrorResponse && error.status === 401) session.expire();
      },
    }),
    takeUntil(session.contextChanged),
  );
};
