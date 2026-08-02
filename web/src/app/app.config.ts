import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { authInterceptor } from './core/auth/auth.interceptor';
import { routes } from './app.routes';
import { provideVetFlowUiKit } from './shared/ui-kit/theme/provide-vetflow-ui-kit';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Every API call carries the bearer token, and a refused token returns the user to the login
    // screen with the ruled message instead of failing silently (REQ-IDN-003, BR-IDN-008).
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideVetFlowUiKit(),
  ],
};
