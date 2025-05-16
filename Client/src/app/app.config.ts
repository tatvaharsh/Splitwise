import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';
import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimationsAsync(),
    provideHttpClient(),
    provideRouter(
      routes,
      withRouterConfig({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled',
      } as any),
      withComponentInputBinding()
    ),
    // {
    //   provide: MAT_CARD_CONFIG,
    //   useValue: {
    //     appearance: 'outlined',
    //   },
    // },
  ],
};