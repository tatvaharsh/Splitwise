// import { HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
// import { AuthService } from '../../services/auth.service';
// import { Router } from '@angular/router';
// import { inject } from '@angular/core';
// import { IResponse } from '../response';
// import { catchError, finalize, switchMap, tap, throwError } from 'rxjs';
// import { LocalStorageService } from '../../services/storage.service';
// import { GlobalConstant } from '../global-const';
// import { LoaderService } from '../../services/loader.service';
// import { ILoginResponse } from '../../models/auth.model';

// export function apiInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
//   const loaderService = inject(LoaderService);
//   const authService = inject(AuthService);
//   const router = inject(Router);
//   const store = inject(LocalStorageService);

//   loaderService.showLoader();
//   const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);

//   const isFormDataRequest = req.body instanceof FormData;

//   let modifiedHeaders: { [header: string]: string } = {
//     Accept: 'application/json, text/plain',
//     'Access-Control-Allow-Headers': 'Content-Type',
//     'Cache-Control': 'no-cache',
//     Pragma: 'no-cache',
//     Authorization: `Bearer ${accessToken}`,
//   };

//   if (!isFormDataRequest) {
//     modifiedHeaders['Content-Type'] = 'application/json';
//   }

//   const modifiedReq = req.clone({
//     setHeaders: modifiedHeaders,
//   });

//   return next(modifiedReq).pipe(
//     tap((event) => {
//       if (event instanceof HttpResponse) {
//         const response: IResponse<any> = event.body as IResponse<any>;
//         if (!response.content || response.message !== 'Success') {
//         }
//       }
//     }),
//     catchError((err) => {
//       if (err.status === 401) {
//         const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);
//         console.log('Access token:', accessToken);
//         if (typeof accessToken === 'string' && accessToken.trim()) {
//           const clonedRequest = req.clone({
//             setHeaders: {
//               Authorization: `Bearer ${accessToken}`,
//             },
//           });
      
//           return next(clonedRequest);
//         } else {
//           router.navigate([`${GlobalConstant.AUTH}/${GlobalConstant.LOGIN}`]);
//           console.log(GlobalConstant.SESSION_EXPIRED);
//         }
//       } else if (err.status === 403) {
//         router.navigate([GlobalConstant.ACCESS_DENIED]);
//       } else {
//        console.log(err.error.message);
//       }
//       return throwError(() => err);
//     }),
//     finalize(() => {
//       loaderService.hideLoader();
//     })
//   );
// }
import { HttpInterceptorFn } from '@angular/common/http';
import { GlobalConstant } from '../global-const';
import { LocalStorageService } from '../../services/storage.service';
import { inject } from '@angular/core';
 
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(LocalStorageService);
  const accessToken = store.get(GlobalConstant.ACCESS_TOKEN);
  const newRequest = req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`,
    },
  });
  return next(newRequest);
};