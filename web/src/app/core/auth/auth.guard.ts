import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * No business screen is reachable without a session (REQ-IDN-006, BR-IDN-005, AC-IDN-009). The
 * Pilot begins at a login screen and there is no anonymous path into the application.
 *
 * `CanMatch` rather than `CanActivate`, so an unauthenticated visit never even downloads the
 * feature's lazy bundle.
 *
 * It checks only that a token is held; whether it is still valid is the server's answer, and the
 * interceptor turns a refusal into a return to the login screen with the ruled message. A client
 * that judged validity for itself would be trusting a value the user can edit.
 */
export const authGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

/**
 * The mirror image: someone who is already signed in has no use for the login screen, and sending
 * them to it after a refresh would look like being signed out.
 */
export const anonymousOnlyGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Same destination as a fresh sign-in (REQ-DSH-001, DEC-DSH-011) — the two must agree, or
  // a refresh would land somewhere a sign-in does not.
  return auth.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
};
