import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../core/i18n/translation.service';

/**
 * The single hint → error → success slot under a field
 * (validation-and-guidance.md STD-UX-013/014/068): the hint shows before any
 * error, the error replaces it, and after a correction the success
 * confirmation replaces the error. The slot reserves its line so the swap
 * never shifts the field under the cursor (STD-UX-104).
 *
 * The element's `id` is what the control's `aria-describedby` points at
 * (STD-UX-091); a new error announces via `role="alert"`, a correction
 * announces politely via `role="status"` (STD-UX-092).
 */
@Component({
  selector: 'vf-validation-message',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="vf-msg" [attr.id]="messageId()">
      @if (error(); as message) {
        <span class="vf-msg-error" role="alert">
          <i class="pi pi-exclamation-circle" aria-hidden="true"></i>{{ message }}
        </span>
      } @else if (success()) {
        <span class="vf-msg-success" role="status">
          <i class="pi pi-check-circle" aria-hidden="true"></i>
          <span class="vf-visually-hidden">{{ t.t('validation.corrected') }}</span>
        </span>
      } @else if (hint(); as hintText) {
        <span class="vf-msg-hint">{{ hintText }}</span>
      }
    </span>
  `,
  styles: `
    /* The reserved line: hint/error/success swap in place (STD-UX-104). */
    .vf-msg {
      display: block;
      min-block-size: 1.25rem;
      font-size: var(--vf-text-caption);
    }

    .vf-msg-error {
      display: inline-flex;
      align-items: center;
      gap: var(--vf-space-1);
      color: var(--vf-danger);
    }

    .vf-msg-success {
      display: inline-flex;
      align-items: center;
      color: var(--vf-success);
    }

    .vf-msg-hint {
      color: var(--vf-text-secondary);
    }

    .pi {
      font-size: 0.75rem;
    }
  `,
})
export class VfValidationMessageComponent {
  protected readonly t = inject(TranslationService);

  readonly messageId = input.required<string>();
  readonly error = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly success = input(false);
}
