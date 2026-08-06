import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { LoginPageComponent } from './login-page.component';

/**
 * The login screen against the approved standard (identity/ui.md S1, AC-IDN-010): the three
 * validation moments, a focusable banner that clears on the next edit, a submit button that is
 * never disabled for invalidity, one message for every failure cause (BR-IDN-003), and the ruled
 * landing screen (REQ-DSH-001 / DEC-DSH-011 — the operational dashboard, which supersedes
 * DEC-IDN-007's «no dashboard is built»).
 */
describe('LoginPageComponent', () => {
  let fixture: ComponentFixture<LoginPageComponent>;
  let http: HttpTestingController;

  async function render(queryParams: Record<string, string> = {}): Promise<void> {
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: new Map(Object.entries(queryParams)) } },
        },
      ],
    });

    // ActivatedRoute's queryParamMap is a ParamMap; a Map has `has`/`get` with the same shape,
    // which is all this screen reads.
    fixture = TestBed.createComponent(LoginPageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const input = (index: number): HTMLInputElement =>
    fixture.nativeElement.querySelectorAll('input')[index];
  const submitButton = (): HTMLButtonElement => fixture.nativeElement.querySelector('button[type="submit"]');
  const banner = (): HTMLElement | null => fixture.nativeElement.querySelector('vf-banner');
  const form = (): HTMLFormElement => fixture.nativeElement.querySelector('form');

  async function type(index: number, value: string): Promise<void> {
    const element = input(index);
    element.value = value;
    element.dispatchEvent(new Event('input'));
    await settle();
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('shows the approved title and the two fields, phone first (identity/ui.md)', async () => {
    await render();

    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent?.trim()).toBe(
      'تسجيل الدخول',
    );
    expect(input(0).getAttribute('type')).toBe('tel');
    expect(input(0).getAttribute('inputmode')).toBe('tel');
    // «الأرقام لليسار» — design language §6.
    expect(input(0).classList.contains('field-input--ltr')).toBe(true);
    // Hidden, and with no reveal button: none was asked for, so none was invented.
    expect(input(1).getAttribute('type')).toBe('password');
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('button').length).toBe(1);
  });

  it('never disables the button for an invalid form (TS-IDN-012, STD-UX-016)', async () => {
    await render();

    expect(submitButton().disabled).toBe(false);

    form().dispatchEvent(new Event('submit'));
    await settle();

    // Rejected submit: still enabled, and nothing was sent.
    expect(submitButton().disabled).toBe(false);
    http.expectNone('/api/v1/auth/login');
  });

  it('answers every failure cause with the one approved message (BR-IDN-003)', async () => {
    await render();
    await type(0, '01001127204');
    await type(1, 'wrong');

    form().dispatchEvent(new Event('submit'));
    await settle();

    http.expectOne('/api/v1/auth/login').flush(
      { type: 'about:blank', title: 'Sign-in failed', status: 401, errorCode: 'VTF-IDN-001' },
      { status: 401, statusText: 'Unauthorized' },
    );
    await settle();

    expect(banner()?.textContent?.trim()).toBe('رقم الهاتف أو كلمة المرور غير صحيح.');
    // The banner takes focus, because the failure has no field to point at (STD-UX-071).
    expect(document.activeElement).toBe(banner());
  });

  it('separates a system failure from wrong credentials (identity/ui.md)', async () => {
    await render();
    await type(0, '01001127204');
    await type(1, 'secret');

    form().dispatchEvent(new Event('submit'));
    await settle();

    http.expectOne('/api/v1/auth/login').flush('', { status: 500, statusText: 'Server Error' });
    await settle();

    expect(banner()?.textContent?.trim()).toBe('تعذّر تسجيل الدخول الآن. أعد المحاولة.');
  });

  it('clears the rejection on the next edit (STD-UX-035)', async () => {
    await render();
    await type(0, '01001127204');
    await type(1, 'wrong');
    form().dispatchEvent(new Event('submit'));
    await settle();
    http.expectOne('/api/v1/auth/login').flush(
      { type: 'about:blank', title: 'Sign-in failed', status: 401, errorCode: 'VTF-IDN-001' },
      { status: 401, statusText: 'Unauthorized' },
    );
    await settle();
    expect(banner()).toBeTruthy();

    await type(1, 'another try');

    expect(banner()).toBeNull();
  });

  it('explains an expired session rather than failing silently (TS-IDN-010, BR-IDN-008)', async () => {
    await render({ expired: '1' });

    expect(banner()?.textContent?.trim()).toBe('انتهت جلستك. سجّل الدخول من جديد.');
  });

  it('lands on the existing product list after signing in (DEC-IDN-007)', async () => {
    await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    await type(0, '01001127204');
    await type(1, '01001127204');
    form().dispatchEvent(new Event('submit'));
    await settle();

    http.expectOne('/api/v1/auth/login').flush({
      accessToken: 'token',
      expiresAt: '2026-08-03T00:00:00Z',
      displayName: 'Clinic Owner',
    });
    await settle();

    expect(navigate).toHaveBeenCalledWith(['/dashboard']);
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(true);
  });
});
