import { HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, finalize, switchMap, tap } from 'rxjs';
import { SnackbarService } from '../../services/snackbar.service';
import { AuthService } from '../../services/auth.service';
import { LocalStorageService } from '../../services/storage.service';
import { GlobalConstant } from '../global-const';
import { IResponse } from '../response';

export function   apiInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
  const snackBar = inject(SnackbarService);
  const authService = inject(AuthService);
  const router = inject(Router);
  const store = inject(LocalStorageService);

  //loaderService.showLoader();
  const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);

  const isFormDataRequest = req.body instanceof FormData;

  // Modify request headers dynamically
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
        if (!response.content || response.message !== 'Success')
          snackBar.showSuccess(response.message);
      }
    }),
    catchError((err) => {
      if (err.status === 401) {
        snackBar.showError('Session expired. Please login again.');
        // authService.logout();
        router.navigate([GlobalConstant.LOGIN]);
      } else if (err.status === 403) {
        router.navigate([GlobalConstant.ACCESS_DENIED]);
      } else {
        snackBar.showError(err.error.message);
      }
      throw err;
    }),
    finalize(() => {
      //loaderService.hideLoader();
    })
  );
}

// import { HttpInterceptorFn } from '@angular/common/http';
// import { GlobalConstant } from '../global-const';
// import { LocalStorageService } from '../../services/storage.service';
// import { inject } from '@angular/core';
 
// export const apiInterceptor: HttpInterceptorFn = (req, next) => {
//   const store = inject(LocalStorageService);
//   const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);
//   const newRequest = req.clone({
//     setHeaders: {
//       Authorization: `Bearer ${accessToken}`,
//     },
//   });
//   return next(newRequest);
// };