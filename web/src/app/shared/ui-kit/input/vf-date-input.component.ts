import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * A labelled native date field driven as a controlled value (`[value]` in,
 * `(valueChange)` out) — the same shape as {@link VfSelectComponent}, so filter
 * panels use it without form binding. The value is the ISO `yyyy-mm-dd` string
 * the native control exposes; an empty field emits `null`.
 *
 * Presentation only (STD-FE-011): it owns its label, its optional required marker,
 * its optional error line (STD-FE-017), and its layout; it holds no state and
 * injects no services. The controlled `[value]`/`(valueChange)` contract is
 * unchanged, so filter panels keep using it without the error inputs.
 */
@Component({
  selector: 'vf-date-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="field">
      <span class="field-caption">
        {{ label() }}
        @if (required()) {
          <span class="field-required" aria-hidden="true">*</span>
        }
      </span>
      <input
        class="field-input"
        [class.field-input--invalid]="!!error()"
        type="date"
        [value]="value() ?? ''"
        [max]="max() ?? ''"
        [min]="min() ?? ''"
        [attr.aria-label]="label()"
        [attr.aria-invalid]="!!error()"
        (input)="onInput($event)"
      />
      @if (error(); as message) {
        <span class="field-error" role="alert">{{ message }}</span>
      }
    </label>
  `,
  styles: `
    .field {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .field-caption {
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .field-required {
      color: var(--vf-danger, #b42318);
      margin-inline-start: 0.125rem;
    }

    .field-input {
      inline-size: 100%;
      font-family: inherit;
      font-size: var(--vf-text-body);
      color: var(--vf-text);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border-strong);
      border-radius: var(--vf-radius-small);
      padding-block: 0.5rem;
      padding-inline: var(--vf-space-3);
      transition: border-color 120ms ease, box-shadow 120ms ease;
    }

    .field-input:focus-visible {
      border-color: var(--vf-primary);
      box-shadow: var(--vf-focus-ring);
      outline: none;
    }

    .field-input--invalid {
      border-color: var(--vf-danger, #b42318);
    }

    .field-error {
      font-size: var(--vf-text-caption);
      color: var(--vf-danger, #b42318);
    }
  `,
})
export class VfDateInputComponent {
  readonly label = input('');
  readonly value = input<string | null>(null);
  /** Optional inclusive bounds (ISO `yyyy-mm-dd`) — e.g. a range's other end. */
  readonly min = input<string | null>(null);
  readonly max = input<string | null>(null);
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly valueChange = output<string | null>();

  protected onInput(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.valueChange.emit(next === '' ? null : next);
  }
}
