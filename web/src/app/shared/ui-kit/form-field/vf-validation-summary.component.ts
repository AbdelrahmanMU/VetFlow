import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { TranslationService } from '../../../core/i18n/translation.service';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';

/**
 * The Validation Summary (validation-and-guidance.md STD-UX-023/129, owner
 * ruling 4): a navigational map for long forms — one linked entry per
 * invalid field; activating an entry focuses and scrolls to the field
 * (STD-UX-076). «خريطة لا كومة نصوص».
 *
 * Placed inside a `form[vfSubmitGuide]`; it renders only after a rejected
 * submit (moment 3) while invalid fields remain. Short single-view forms
 * simply do not include it — their inline errors are already all visible.
 */
@Component({
  selector: 'vf-validation-summary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (show()) {
      <div class="vf-summary" role="alert">
        <p class="vf-summary-title">{{ t.t('validation.summary.title') }}</p>
        <ul class="vf-summary-list">
          @for (entry of entries(); track entry.controlId) {
            <li>
              <button type="button" class="vf-summary-link" (click)="go(entry.controlId)">
                {{ entry.label }}
              </button>
            </li>
          }
        </ul>
      </div>
    }
  `,
  styles: `
    .vf-summary {
      background: var(--vf-danger-soft);
      color: var(--vf-danger);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-3);
      font-size: var(--vf-text-caption);
    }

    .vf-summary-title {
      margin: 0;
      font-weight: 600;
    }

    .vf-summary-list {
      margin: var(--vf-space-1) 0 0;
      padding-inline-start: var(--vf-space-4);
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .vf-summary-link {
      font-family: inherit;
      font-size: inherit;
      color: inherit;
      background: none;
      border: 0;
      padding: 0;
      text-decoration: underline;
      cursor: pointer;
    }
  `,
})
export class VfValidationSummaryComponent {
  protected readonly t = inject(TranslationService);
  private readonly guide = inject(SubmitGuidanceDirective, { optional: true });
  private readonly focus = inject(ValidationFocusService);

  protected readonly entries = computed(
    () =>
      this.guide
        ?.invalidFields()
        .map((field) => ({ controlId: field.controlId, label: field.label() })) ?? [],
  );

  protected readonly show = computed(
    () => (this.guide?.submitted() ?? false) && this.entries().length > 0,
  );

  protected go(controlId: string): void {
    this.focus.focusControlId(controlId);
  }
}
