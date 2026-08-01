import { ChangeDetectionStrategy, Component, computed, forwardRef, inject, input, output, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { VfFormFieldComponent } from '../form-field/vf-form-field.component';

/**
 * A labelled native date field. Two binding modes, both supported:
 *
 * - The legacy controlled pair (`[value]` in, `(valueChange)` out) — filter
 *   panels keep using it without form binding.
 * - A ControlValueAccessor for `[formControl]` binding (STD-FE-016) — the
 *   validation-foundation repair: the field now fires `touched` on blur, so
 *   moment 2 (validation-and-guidance.md §3) works for dates exactly as for
 *   text. Inside a `vf-form-field` the wrapper owns label, message line, and
 *   timing (STD-UX-120).
 *
 * The value is the ISO `yyyy-mm-dd` string the native control exposes; an
 * empty field emits `null`.
 */
@Component({
  selector: 'vf-date-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => VfDateInputComponent), multi: true },
  ],
  template: `
    <label class="field">
      @if (!formField) {
        <span class="field-caption">
          {{ label() }}
          @if (required()) {
            <span class="field-required" aria-hidden="true">*</span>
          }
        </span>
      }
      <input
        class="field-input"
        [class.field-input--invalid]="isInvalid()"
        type="date"
        [value]="displayValue() ?? ''"
        [max]="max() ?? ''"
        [min]="min() ?? ''"
        [disabled]="disabled()"
        [attr.id]="formField?.controlId ?? null"
        [attr.aria-label]="formField ? null : label()"
        [attr.aria-describedby]="formField?.messageId ?? null"
        [attr.aria-invalid]="isInvalid()"
        (input)="onInput($event)"
        (blur)="onTouched()"
      />
      @if (!formField && error(); as message) {
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

    .field-input:disabled {
      opacity: 0.55;
      cursor: default;
    }

    .field-error {
      font-size: var(--vf-text-caption);
      color: var(--vf-danger, #b42318);
    }
  `,
})
export class VfDateInputComponent implements ControlValueAccessor {
  protected readonly formField = inject(VfFormFieldComponent, { optional: true });

  readonly label = input('');
  readonly value = input<string | null>(null);
  /** Optional inclusive bounds (ISO `yyyy-mm-dd`) — e.g. a range's other end. */
  readonly min = input<string | null>(null);
  readonly max = input<string | null>(null);
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly valueChange = output<string | null>();

  /** CVA-managed value; active once a form binding registered (formBound). */
  private readonly cvaValue = signal<string | null>(null);
  private readonly formBound = signal(false);
  protected readonly disabled = signal(false);

  protected readonly displayValue = computed(() => (this.formBound() ? this.cvaValue() : this.value()));

  protected readonly isInvalid = computed(() =>
    this.formField ? this.formField.invalid() : !!this.error(),
  );

  private onChange: (value: string | null) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(next: string | null): void {
    this.cvaValue.set(next);
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.formBound.set(true);
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const next = raw === '' ? null : raw;
    this.cvaValue.set(next);
    this.valueChange.emit(next);
    this.onChange(next);
  }
}
