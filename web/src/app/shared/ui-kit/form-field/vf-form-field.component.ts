import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  contentChild,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { NgControl, ValueChangeEvent } from '@angular/forms';

import { TranslationService } from '../../../core/i18n/translation.service';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import {
  RuleMessageOverrides,
  resolveValidationMessage,
} from '../../../core/validation/validation-messages';
import { VfValidationMessageComponent } from './vf-validation-message.component';

let nextFieldId = 0;

/**
 * The single field wrapper (validation-and-guidance.md §13 item 1,
 * STD-UX-120): label, projected control, and the hint → error → success
 * slot. It owns the three validation moments (§3), the message resolution
 * (STD-UX-017), and the aria wiring (`for`/`id`, `aria-describedby`,
 * `aria-invalid` — STD-UX-090/091/093), so no screen re-implements them.
 *
 * Timing (owner ruling 2): nothing judges unfinished input — an error shows
 * only once the control is touched (blur, moment 2) or the form's guidance
 * ran (submit, moment 3) — and it disappears on the exact input event that
 * fixes it (STD-UX-015). A projected server error (`errors.server`) clears
 * on the next edit. A field that recovers from an error shows the success
 * confirmation (STD-UX-014).
 */
@Component({
  selector: 'vf-form-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfValidationMessageComponent],
  template: `
    <div
      class="vf-field"
      [class.vf-field--invalid]="invalid()"
      [class.vf-field--success]="showSuccess()"
    >
      <label class="vf-field-label" [attr.for]="controlId">
        {{ label() }}
        @if (required()) {
          <span class="vf-field-required" aria-hidden="true">*</span>
        }
      </label>
      <ng-content />
      <vf-validation-message
        [messageId]="messageId"
        [error]="errorText()"
        [hint]="hint()"
        [success]="showSuccess()"
      />
    </div>
  `,
  styles: `
    .vf-field {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      /* Headroom when the focus service scrolls the field into view (STD-UX-070). */
      scroll-margin-block: var(--vf-space-7);
    }

    .vf-field-label {
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .vf-field-required {
      color: var(--vf-danger);
      margin-inline-start: 0.125rem;
    }
  `,
})
export class VfFormFieldComponent {
  private readonly t = inject(TranslationService);
  private readonly guide = inject(SubmitGuidanceDirective, { optional: true });
  private readonly destroyRef = inject(DestroyRef);

  readonly label = input.required<string>();
  readonly required = input(false);
  /** Hint copy shown before any error (STD-UX-013); comes from the catalog like all copy. */
  readonly hint = input<string | null>(null);
  /** Ruled contextual wordings per rule (STD-UX-111) — e.g. `{ required: 'adjustment.error.productRequired' }`. */
  readonly messages = input<RuleMessageOverrides | undefined>(undefined);

  readonly controlId = `vf-field-${nextFieldId++}`;
  readonly messageId = `${this.controlId}-message`;

  private readonly ngControl = contentChild(NgControl);

  /** Bumped on every control event so computeds re-read the control snapshot. */
  private readonly stateVersion = signal(0);
  private readonly hadError = signal(false);

  /**
   * Re-arms the subscription effect for the one non-reactive gap: the query
   * can resolve while the directive's `control` is still null (observed with
   * dialog-hosted content, where the projected form initializes inside the
   * dialog's own creation pass). `NgControl.control` is not a signal, so the
   * effect re-checks one microtask later; the window closes after init, which
   * bounds the retries.
   */
  private readonly controlRetry = signal(0);

  /** Moment gate: the control spoke for itself (blur) or the form's guidance ran (submit). */
  private readonly revealed = computed(() => {
    this.stateVersion();
    const control = this.ngControl()?.control;
    return !!control && (control.touched || (this.guide?.submitted() ?? false));
  });

  readonly invalid = computed(() => {
    this.stateVersion();
    const control = this.ngControl()?.control;
    return !!control && control.invalid && this.revealed();
  });

  readonly errorText = computed(() => {
    // Read the version directly: `invalid()` alone would keep this computed
    // frozen when the *violated rule* changes while the field stays invalid
    // (required → maxlength), because computeds only re-run when a
    // dependency's value changes — and `true` stays `true`.
    this.stateVersion();
    if (!this.invalid()) {
      return null;
    }

    const control = this.ngControl()?.control;
    const resolved = resolveValidationMessage(control?.errors ?? null, this.messages());
    return resolved ? this.t.t(resolved.key, resolved.params) : null;
  });

  /** Success after correction only — a field that never erred stays quiet (STD-UX-014). */
  readonly showSuccess = computed(() => {
    this.stateVersion();
    const control = this.ngControl()?.control;
    return !!control && this.hadError() && control.valid && this.revealed();
  });

  constructor() {
    effect((onCleanup) => {
      this.controlRetry();
      const directive = this.ngControl();
      if (!directive) {
        // No projected control yet — the query signal re-runs this effect.
        return;
      }

      const control = directive.control;
      if (!control) {
        queueMicrotask(() => this.controlRetry.update((version) => version + 1));
        return;
      }

      const subscription = control.events.subscribe((event) => {
        this.stateVersion.update((version) => version + 1);
        if (event instanceof ValueChangeEvent) {
          this.clearServerError();
        }

        // A reset control starts a fresh cycle: no success chrome carried over.
        if (control.pristine && control.untouched) {
          this.hadError.set(false);
        }
      });
      onCleanup(() => subscription.unsubscribe());
    });

    effect(() => {
      if (this.invalid()) {
        this.hadError.set(true);
      }
    });

    const unregister = this.guide?.register({
      controlId: this.controlId,
      label: () => this.label(),
      invalid: computed(() => {
        this.stateVersion();
        return !!this.ngControl()?.control?.invalid;
      }),
    });
    if (unregister) {
      this.destroyRef.onDestroy(unregister);
    }
  }

  /** A projected server error is stale the moment the user edits (STD-UX-015/019). */
  private clearServerError(): void {
    const control = this.ngControl()?.control;
    if (!control?.errors?.['server']) {
      return;
    }

    const rest = { ...control.errors };
    delete rest['server'];
    control.setErrors(Object.keys(rest).length > 0 ? rest : null);
  }
}
