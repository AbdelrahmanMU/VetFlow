import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Route, UrlTree, provideRouter } from '@angular/router';

import { AuthService } from './auth.service';
import { anonymousOnlyGuard, authGuard } from './auth.guard';

/**
 * No business screen without a session (REQ-IDN-006, BR-IDN-005, AC-IDN-005/009). The Pilot begins
 * at a login screen and there is no anonymous path into the application.
 */
describe('auth guards', () => {
  const route: Route = { path: '' };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => localStorage.clear());

  function run(guard: typeof authGuard): boolean | UrlTree {
    return TestBed.runInInjectionContext(() => guard(route, [])) as boolean | UrlTree;
  }

  it('sends a visitor with no session to the login screen', () => {
    const result = run(authGuard);

    expect(result instanceof UrlTree).toBe(true);
    expect(String(result)).toBe('/login');
  });

  it('lets a signed-in user through', () => {
    TestBed.inject(AuthService);
    localStorage.setItem(
      'vetflow.session',
      JSON.stringify({ accessToken: 'the-token', displayName: 'Clinic Owner' }),
    );

    // The service reads storage when it is constructed, so a fresh injector is what a reload does.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });

    expect(run(authGuard)).toBe(true);
  });

  it('keeps a signed-in user off the login screen', () => {
    localStorage.setItem(
      'vetflow.session',
      JSON.stringify({ accessToken: 'the-token', displayName: 'Clinic Owner' }),
    );

    const result = run(anonymousOnlyGuard);

    expect(String(result)).toBe('/dashboard');
  });

  it('lets a signed-out visitor reach the login screen', () => {
    expect(run(anonymousOnlyGuard)).toBe(true);
  });
});
