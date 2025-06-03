import { HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, tap, throwError } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { LocalStorageService } from '../../services/storage.service';
import { GlobalConstant } from '../global-const';
import { IResponse } from '../response';

export function apiInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
  const authService = inject(AuthService);
  const router = inject(Router);
  const store = inject(LocalStorageService);

  const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);

  const isFormDataRequest = req.body instanceof FormData;

  let modifiedHeaders: { [header: string]: string } = {
    Accept: 'application/json, text/plain',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Cache-Control': 'no-cache',
    Pragma: 'no-cache',
    Authorization: `Bearer ${accessToken}`,
  };

  if (!isFormDataRequest) {
    modifiedHeaders['Content-Type'] = 'application/json';
  }

  const modifiedReq = req.clone({
    setHeaders: modifiedHeaders,
  });

  return next(modifiedReq).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        const response: IResponse<any> = event.body as IResponse<any>;
      }
    }),
    catchError((error) => {
      console.error('API Error:', error);
      return throwError(() => error);
    })
  );
}
