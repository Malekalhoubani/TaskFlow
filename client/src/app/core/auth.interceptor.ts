import {
  HttpBackend,
  HttpClient,
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError, tap } from 'rxjs';
import { AuthResult } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const rawHttp = new HttpClient(inject(HttpBackend));
  const token = sessionStorage.getItem('taskflow_access_token');
  const authorized = token
    ? request.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
        withCredentials: true,
      })
    : request.clone({ withCredentials: true });

  return next(authorized).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status !== 401 ||
        request.url.includes('/auth/login') ||
        request.url.includes('/auth/refresh')
      )
        return throwError(() => error);
      return rawHttp
        .post<AuthResult>('https://localhost:7001/api/auth/refresh', {}, { withCredentials: true })
        .pipe(
          tap((result) => {
            if (result.accessToken)
              sessionStorage.setItem('taskflow_access_token', result.accessToken);
          }),
          switchMap(() => {
            const refreshedToken = sessionStorage.getItem('taskflow_access_token');
            return next(
              request.clone({
                setHeaders: refreshedToken ? { Authorization: `Bearer ${refreshedToken}` } : {},
                withCredentials: true,
              }),
            );
          }),
          catchError((refreshError) => {
            sessionStorage.removeItem('taskflow_access_token');
            return throwError(() => refreshError);
          }),
        );
    }),
  );
};
