import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/** Query parameter that carries the reason to the login screen (BR-IDN-008 — never a silent failure). */
export const SESSION_EXPIRED_PARAM = 'expired';

/**
 * Attaches the bearer token to every API call, and turns a rejected token into a visible return to
 * the login screen (REQ-IDN-003, BR-IDN-008).
 *
 * <b>Sign-in itself is left alone</b>: it is the one anonymous endpoint (REQ-IDN-006), and a 401
 * from it means "those credentials are wrong", not "your session ended". Redirecting on it would
 * replace the ruled `login.error.invalid` message with a page navigation.
 *
 * <b>Expiry is distinguished on the client, not by a second server code.</b> If a token was
 * attached and the server still refused it, the session ended — so the user is told so. A request
 * made with no token at all was never a session, and simply lands on the login screen. This keeps
 * the ruled distinction (identity/requirements.md) without inventing a contract for it.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.accessToken();
  const isSignIn = request.url.includes('/auth/login');

  const authorized =
    token && !isSignIn
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isSignIn) {
        // The token is discarded first: leaving a rejected token in place would re-attach it to
        // every later request and produce a redirect loop.
        const hadSession = token !== null;
        auth.signOut();
        void router.navigate(['/login'], {
          queryParams: hadSession ? { [SESSION_EXPIRED_PARAM]: '1' } : {},
        });
      }

      return throwError(() => error);
    }),
  );
};
