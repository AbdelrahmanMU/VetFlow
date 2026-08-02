import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

/**
 * The bearer token on every call, and a refused token as a visible return to the login screen
 * (REQ-IDN-003, BR-IDN-008, AC-IDN-008) — never a blank page and never a silent failure.
 */
describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
  });

  function signIn(): void {
    auth.signIn('01001127204', '01001127204').subscribe();
    backend.expectOne('/api/v1/auth/login').flush({
      accessToken: 'the-token',
      expiresAt: '2026-08-03T00:00:00Z',
      displayName: 'Clinic Owner',
    });
  }

  it('attaches the token to a business call', () => {
    signIn();

    http.get('/api/v1/products').subscribe();

    const request = backend.expectOne('/api/v1/products');
    expect(request.request.headers.get('Authorization')).toBe('Bearer the-token');
    request.flush({});
  });

  it('never attaches one to sign-in — the one anonymous endpoint (REQ-IDN-006)', () => {
    signIn();

    auth.signIn('01001127204', 'again').subscribe();

    const request = backend.expectOne('/api/v1/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ accessToken: 't', expiresAt: '', displayName: 'x' });
  });

  it('ends the session and explains it when a held token is refused (TS-IDN-010, BR-IDN-008)', () => {
    signIn();
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/v1/products').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/products').flush('', { status: 401, statusText: 'Unauthorized' });

    // The token is discarded, or it would be re-attached forever and loop.
    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { expired: '1' } });
  });

  it('sends someone who never had a session to the login screen without the expiry message', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    http.get('/api/v1/products').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/products').flush('', { status: 401, statusText: 'Unauthorized' });

    // "Your session ended" would be untrue: there was never a session.
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: {} });
  });

  it('leaves a failed sign-in on the screen instead of navigating (BR-IDN-003)', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    auth.signIn('01001127204', 'wrong').subscribe({ error: () => undefined });
    backend.expectOne('/api/v1/auth/login').flush('', { status: 401, statusText: 'Unauthorized' });

    // A navigation here would replace the ruled `login.error.invalid` message with a page change.
    expect(navigate).not.toHaveBeenCalled();
  });
});
