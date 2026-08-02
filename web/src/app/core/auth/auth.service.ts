import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { ApiClient } from '../api/api-client';

/** The sign-in response (identity/requirements.md — API contract). */
export interface SignInResponse {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly displayName: string;
}

interface StoredSession {
  readonly accessToken: string;
  readonly displayName: string;
}

const STORAGE_KEY = 'vetflow.session';

/**
 * The client's half of the session (REQ-IDN-003/004, BR-IDN-009).
 *
 * <b>One access token, no refresh and no "remember me"</b> (ADR-0022 §7): the session ends by an
 * explicit sign-out or when the token expires, and by nothing else. That is also why the token is
 * kept in `localStorage` rather than in memory — a page refresh is not a sign-out, and losing the
 * session on one would end it by a third route the rules do not allow.
 *
 * <b>The tenant is never held here.</b> It lives inside the token and the server reads it from the
 * token's claims alone (BR-IDN-004, ADR-0022 §12.5); a tenant the client could hold is a tenant the
 * client could change.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);

  private readonly session = signal<StoredSession | null>(readStoredSession());

  /** Whether a token is held. Not a claim that it is still valid — the server decides that. */
  readonly isAuthenticated = computed(() => this.session() !== null);

  readonly displayName = computed(() => this.session()?.displayName ?? '');

  readonly accessToken = computed(() => this.session()?.accessToken ?? null);

  signIn(phoneNumber: string, password: string): Observable<SignInResponse> {
    return this.api
      .post<SignInResponse>('/auth/login', { phoneNumber, password })
      .pipe(tap((response) => this.store(response)));
  }

  /**
   * Ends the session on the client. There is no server call, because there is nothing on the
   * server to end: no refresh token and no server-side session exist (DEC-IDN-003), and inventing
   * a request that does nothing would misrepresent what happens.
   */
  signOut(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private store(response: SignInResponse): void {
    const session: StoredSession = {
      accessToken: response.accessToken,
      displayName: response.displayName,
    };

    this.session.set(session);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }
}

function readStoredSession(): StoredSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<StoredSession>;
    return typeof parsed.accessToken === 'string' && typeof parsed.displayName === 'string'
      ? { accessToken: parsed.accessToken, displayName: parsed.displayName }
      : null;
  } catch {
    // A corrupted entry is treated as no session at all: the user signs in again, which is the
    // only recovery path this phase has (BR-IDN-010 — there is no reset flow).
    return null;
  }
}
