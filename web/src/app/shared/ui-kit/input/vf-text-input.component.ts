import { ChangeDetectionStrategy, Component, computed, forwardRef, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { VfFormFieldComponent } from '../form-field/vf-form-field.component';

/**
 * Labelled single-line text field for reactive forms (STD-FE-016): a
 * ControlValueAccessor so features bind it with `[formControl]`.
 *
 * Inside a `vf-form-field` (STD-UX-120) the wrapper owns the label, the
 * message line, and the timing; this component then renders only the input,
 * wired to the wrapper's `for`/`id` and `aria-describedby` (STD-UX-091).
 * Standalone (legacy screens, until their phase migrates them) it keeps its
 * own label and error line (STD-FE-017).
 */
@Component({
  selector: 'vf-text-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => VfTextInputComponent), multi: true },
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
        [class.field-input--ltr]="digitsFirst()"
        [type]="type()"
        [value]="value()"
        [placeholder]="placeholder()"
        [disabled]="disabled()"
        [attr.id]="formField?.controlId ?? null"
        [attr.inputmode]="inputMode() || null"
        [attr.autocomplete]="autocomplete() || null"
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

    /* «الأرقام لليسار» (design language §6) applied to a field whose content is
       digits: the value reads left-to-right and sits on the left edge, while the
       label and the rest of the screen stay RTL. */
    .field-input--ltr {
      direction: ltr;
      text-align: left;
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
export class VfTextInputComponent implements ControlValueAccessor {
  protected readonly formField = inject(VfFormFieldComponent, { optional: true });

  readonly label = input('');
  readonly placeholder = input('');
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly type = input<'text' | 'search' | 'password' | 'tel'>('text');

  /** `inputmode`, so a phone field opens the numeric keypad on a touch device (identity/ui.md). */
  readonly inputMode = input('');

  readonly autocomplete = input('');

  /**
   * Renders the value left-to-right and left-aligned: design language §6's «الأرقام لليسار» for a
   * field whose content is digits. It is presentation only — the value is unchanged.
   */
  readonly digitsFirst = input(false);

  protected readonly value = signal('');
  protected readonly disabled = signal(false);

  protected readonly isInvalid = computed(() =>
    this.formField ? this.formField.invalid() : !!this.error(),
  );

  private onChange: (value: string) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(event: Event): void {
    const next = (event.target as HTMLInputElement).value;
    this.value.set(next);
    this.onChange(next);
  }
}
