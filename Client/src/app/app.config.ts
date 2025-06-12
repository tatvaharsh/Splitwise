import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';
import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { apiInterceptor } from './generic/interceptors/api.interceptor';
import { provideToastr } from 'ngx-toastr';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimationsAsync(),
    provideToastr(),
    provideHttpClient(withFetch(), withInterceptors([apiInterceptor])),
    provideRouter(
      routes,
      withRouterConfig({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled',
      } as any),
      withComponentInputBinding()
    )
  ],
};