import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { SESSION_EXPIRED_PARAM } from '../../../core/auth/auth.interceptor';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ApiErrorMapper } from '../../../core/validation/api-error-mapper';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';
import { vfValidators } from '../../../core/validation/validators';
import { VfBannerComponent } from '../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfFormFieldComponent } from '../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfLogoComponent } from '../../../shared/ui-kit/logo/vf-logo.component';
import { MessageKey } from '../../../core/i18n/ar';

/**
 * شاشة الدخول (identity/ui.md S1) — the only screen of the Identity module, the first thing a user
 * sees, and the only screen outside the application shell: there is no navigation to offer someone
 * who is not signed in.
 *
 * <b>One failure message for every cause</b> (BR-IDN-003): an unknown phone number, a wrong
 * password and a user with no membership are indistinguishable here, because telling them apart
 * tells an attacker which numbers are registered. The backend already answers all three with one
 * code; this screen renders that one message and never elaborates.
 *
 * It follows the approved validation standard exactly: the three moments (no error while first
 * typing, error on leaving a field, focus to the first invalid field on submit), a focusable
 * `vf-banner` that clears on the next edit (STD-UX-035/071), and a submit button that is
 * <b>never disabled for invalidity</b> (STD-UX-016) — only while the request is in flight.
 *
 * After a successful sign-in it lands on the existing product list. <b>No dashboard is built</b> —
 * the owner ruled that «lands in the app» means the screen that already exists (DEC-IDN-007).
 */
@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfButtonComponent,
    VfFormFieldComponent,
    VfTextInputComponent,
    VfLogoComponent,
  ],
  template: `
    <main class="login">
      <div class="card">
        <vf-logo [height]="40" />
        <h1 class="title">{{ t.t('login.title') }}</h1>

        <form class="form" [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="submit()">
          @if (failureMessage(); as message) {
            <vf-banner tone="error" #failureBanner>{{ message }}</vf-banner>
          }

          <vf-form-field [label]="t.t('login.phone')" [required]="true">
            <vf-text-input
              [formControl]="form.controls.phoneNumber"
              type="tel"
              inputMode="tel"
              autocomplete="username"
              [digitsFirst]="true"
            />
          </vf-form-field>

          <vf-form-field [label]="t.t('login.password')" [required]="true">
            <vf-text-input
              [formControl]="form.controls.password"
              type="password"
              autocomplete="current-password"
            />
          </vf-form-field>

          <vf-button variant="primary" type="submit" [full]="true" [disabled]="submitting()">
            {{ submitting() ? t.t('login.submitting') : t.t('login.submit') }}
          </vf-button>
        </form>
      </div>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
    </main>
  `,
  styles: `
    .login {
      min-block-size: 100dvh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--vf-space-5);
      background: var(--vf-bg);
    }

    .card {
      inline-size: 100%;
      max-inline-size: 24rem;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--vf-space-4);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-6) var(--vf-space-5);
    }

    .title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .form {
      inline-size: 100%;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
    }

    @media (max-width: 480px) {
      .login {
        padding: var(--vf-space-3);
      }

      .card {
        padding: var(--vf-space-5) var(--vf-space-4);
      }
    }
  `,
})
export class LoginPageComponent {
  protected readonly t = inject(TranslationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly errors = inject(ApiErrorMapper);
  private readonly focus = inject(ValidationFocusService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  // Only the two documented validations (BR-IDN-003 §Validations): both required, nothing more.
  // No password complexity rule — there is no screen that changes a password, so a policy would
  // have no point of enforcement and would only reject correct credentials.
  protected readonly form = new FormGroup({
    phoneNumber: new FormControl('', { nonNullable: true, validators: vfValidators.required }),
    password: new FormControl('', { nonNullable: true, validators: vfValidators.required }),
  });

  protected readonly submitting = signal(false);

  private readonly failureKey = signal<MessageKey | null>(
    // Arriving here because a token was refused mid-work is explained, never silent (BR-IDN-008).
    null,
  );

  private readonly failureBanner = viewChild('failureBanner', { read: ElementRef });

  protected readonly failureMessage = computed(() => {
    const key = this.failureKey();
    return key ? this.t.t(key) : null;
  });

  protected readonly announcement = computed(() => (this.submitting() ? this.t.t('login.submitting') : ''));

  constructor() {
    if (this.route.snapshot.queryParamMap.has(SESSION_EXPIRED_PARAM)) {
      this.failureKey.set('session.expired');
    }

    // «أوّل تركيز عند فتح الشاشة» on the phone field (identity/ui.md). Done in code rather than
    // with the `autofocus` attribute, which the accessibility lint rules out — and this way the
    // expiry banner, which takes focus of its own, still wins when there is one to read.
    afterNextRender(() => {
      if (!this.failureMessage()) {
        this.host.nativeElement.querySelector('input')?.focus();
      }
    });

    // A rejection never survives the edit that addresses it (STD-UX-035).
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.failureKey.set(null));

    // The failure has no field to point at — the banner takes focus itself (STD-UX-071).
    effect(() => {
      if (!this.failureMessage()) {
        return;
      }

      const banner = this.failureBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    const { phoneNumber, password } = this.form.getRawValue();
    this.submitting.set(true);

    this.auth.signIn(phoneNumber.trim(), password).subscribe({
      next: () => {
        this.submitting.set(false);
        // The landing screen is the product list that already exists (DEC-IDN-007).
        void this.router.navigate(['/catalog/products']);
      },
      error: (error: unknown) => {
        this.submitting.set(false);

        // Classified by error code through the shared mapper (STD-UX-123), so the wrong-credentials
        // message and the "cannot sign in right now" message can never be confused: the first is
        // the server's single sign-in code, the second is anything else — a network failure, a 500.
        const failure = this.errors.map(error, { system: 'login.error.system' });
        this.failureKey.set(failure.code === 'VTF-IDN-001' ? 'login.error.invalid' : 'login.error.system');
      },
    });
  }
}
