import { ChangeDetectionStrategy, Component, computed, forwardRef, inject, input, output, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { VfFormFieldComponent } from '../form-field/vf-form-field.component';

let nextCheckboxId = 0;

/**
 * Labelled checkbox with an always-visible focus ring (design language §14).
 * Two binding modes, both supported:
 *
 * - The legacy controlled pair (`[checked]` in, `(toggled)` out) — filter
 *   drawers keep using it without form binding.
 * - A ControlValueAccessor for `[formControl]` binding (STD-FE-016) — the
 *   validation-foundation repair (validation-and-guidance.md §13 item 8,
 *   under the v1 freeze's accessibility exception): the input now carries an
 *   explicit `id`/`for` label association (STD-UX-093), fires `touched` on
 *   blur, and exposes the error channel — `aria-invalid` and
 *   `aria-describedby` — so a checkbox can point at the message that
 *   explains its invalid state (STD-UX-090/091).
 */
@Component({
  selector: 'vf-checkbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => VfCheckboxComponent), multi: true },
  ],
  template: `
    <label class="checkbox" [attr.for]="checkboxId">
      <input
        type="checkbox"
        class="checkbox-input"
        [id]="checkboxId"
        [checked]="displayChecked()"
        [disabled]="isDisabled()"
        [attr.aria-invalid]="isInvalid() ? true : null"
        [attr.aria-describedby]="describedBy() ?? formField?.messageId ?? null"
        (change)="onToggle($event)"
        (blur)="onTouched()"
      />
      <span class="checkbox-label"><ng-content /></span>
    </label>
  `,
  styles: `
    .checkbox {
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      cursor: pointer;
      padding-block: var(--vf-space-1);
    }

    .checkbox-input {
      inline-size: 1rem;
      block-size: 1rem;
      accent-color: var(--vf-primary);
      cursor: pointer;
    }

    .checkbox-label {
      font-size: var(--vf-text-body);
      color: var(--vf-text);
    }

    .checkbox:has(.checkbox-input:disabled) {
      opacity: 0.55;
      cursor: default;
    }
  `,
})
export class VfCheckboxComponent implements ControlValueAccessor {
  protected readonly formField = inject(VfFormFieldComponent, { optional: true });

  readonly checked = input(false);
  readonly disabled = input(false);
  /** Error-channel override for checkboxes outside a `vf-form-field` (STD-UX-090). */
  readonly invalid = input(false);
  /** `aria-describedby` target for the message explaining an invalid state (STD-UX-091). */
  readonly describedBy = input<string | null>(null);
  readonly toggled = output<void>();

  readonly checkboxId = `vf-checkbox-${nextCheckboxId++}`;

  /** CVA-managed value; active once a form binding registered (formBound). */
  private readonly cvaValue = signal(false);
  private readonly formBound = signal(false);
  private readonly cvaDisabled = signal(false);

  protected readonly displayChecked = computed(() => (this.formBound() ? this.cvaValue() : this.checked()));
  protected readonly isDisabled = computed(() => this.cvaDisabled() || this.disabled());

  protected readonly isInvalid = computed(() =>
    this.formField ? this.formField.invalid() : this.invalid(),
  );

  private onChange: (value: boolean) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  writeValue(next: boolean | null): void {
    this.cvaValue.set(next ?? false);
  }

  registerOnChange(fn: (value: boolean) => void): void {
    this.formBound.set(true);
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.cvaDisabled.set(isDisabled);
  }

  protected onToggle(event: Event): void {
    const next = (event.target as HTMLInputElement).checked;
    this.cvaValue.set(next);
    this.onChange(next);
    this.toggled.emit();
  }
}
