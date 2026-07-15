import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Labelled numeric field for reactive forms (STD-FE-016): a
 * ControlValueAccessor whose model value is a `number | null` (empty ⇒ null).
 * Owns its label, required marker, and error line (STD-FE-017).
 */
@Component({
  selector: 'vf-number-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => VfNumberInputComponent), multi: true },
  ],
  template: `
    <label class="field">
      <span class="field-caption">
        {{ label() }}
        @if (required()) {
          <span class="field-required" aria-hidden="true">*</span>
        }
      </span>
      <input
        class="field-input vf-num"
        [class.field-input--invalid]="!!error()"
        type="number"
        inputmode="decimal"
        [value]="display()"
        [min]="min()"
        [step]="step()"
        [placeholder]="placeholder()"
        [disabled]="disabled()"
        [attr.aria-label]="label()"
        [attr.aria-invalid]="!!error()"
        (input)="onInput($event)"
        (blur)="onTouched()"
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
export class VfNumberInputComponent implements ControlValueAccessor {
  readonly label = input('');
  readonly placeholder = input('');
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly min = input<number | null>(null);
  readonly step = input<number | string>('any');

  protected readonly value = signal<number | null>(null);
  protected readonly disabled = signal(false);

  protected readonly display = () => {
    const current = this.value();
    return current === null ? '' : String(current);
  };

  private onChange: (value: number | null) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(value: number | null): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value.trim();
    const next = raw === '' ? null : Number(raw);
    const normalized = next === null || Number.isNaN(next) ? null : next;
    this.value.set(normalized);
    this.onChange(normalized);
  }
}
