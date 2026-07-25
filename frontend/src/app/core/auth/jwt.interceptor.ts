import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';
import { AuthTokenService } from './auth-token.service';

export const jwtInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const token = inject(AuthTokenService).getToken();
  const authenticatedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAuthenticationEndpoint(request.url)
      ) {
        authService.expireSession();
      }

      return throwError(() => error);
    }),
  );
};

function isAuthenticationEndpoint(url: string): boolean {
  return /\/auth\/(?:login|register)(?:[/?#]|$)/.test(url);
}
