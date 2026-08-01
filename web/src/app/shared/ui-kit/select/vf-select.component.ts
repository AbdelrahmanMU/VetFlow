import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  forwardRef,
  inject,
  input,
  model,
} from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SelectModule } from 'primeng/select';

import { VfFormFieldComponent } from '../form-field/vf-form-field.component';

export interface VfSelectOption<T> {
  readonly label: string;
  readonly value: T;
}

/**
 * Searchable single select (catalog ui.md §12) over a typed reactive control
 * (STD-FE-016). Wraps the component foundation; features never import it
 * directly (ADR-0012).
 *
 * Two binding modes, both supported: the legacy controlled `[value]`/
 * `(valueChange)` pair (filter panels), and — since the validation
 * foundation — a ControlValueAccessor for `[formControl]` binding inside a
 * `vf-form-field`, which then owns label, message line, and timing
 * (STD-UX-120). Touched fires on blur and on panel close, so moment 2 never
 * triggers while the user is still choosing.
 */
@Component({
  selector: 'vf-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => VfSelectComponent), multi: true },
  ],
  template: `
    <div class="select-label">
      @if (!formField) {
        <span class="select-caption">
          {{ label() }}
          @if (required()) {
            <span class="select-required" aria-hidden="true">*</span>
          }
        </span>
      }
      <p-select
        [formControl]="control"
        [options]="mutableOptions()"
        optionLabel="label"
        optionValue="value"
        [filter]="filterable()"
        [showClear]="clearable()"
        [placeholder]="placeholder()"
        appendTo="body"
        styleClass="vf-select"
        [class.vf-select--invalid]="isInvalid()"
        [inputId]="formField?.controlId"
        [ariaLabel]="formField ? undefined : label()"
        (onBlur)="markTouched()"
        (onHide)="markTouched()"
      />
      @if (!formField && error(); as message) {
        <span class="select-error" role="alert">{{ message }}</span>
      }
    </div>
  `,
  styles: `
    .select-label {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .select-caption {
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .select-required {
      color: var(--vf-danger, #b42318);
      margin-inline-start: 0.125rem;
    }

    .select-error {
      font-size: var(--vf-text-caption);
      color: var(--vf-danger, #b42318);
    }

    ::ng-deep .vf-select {
      inline-size: 100%;
      font-family: var(--vf-font);
    }

    ::ng-deep .vf-select.vf-select--invalid {
      border-color: var(--vf-danger, #b42318);
    }
  `,
})
export class VfSelectComponent<T> implements ControlValueAccessor {
  protected readonly formField = inject(VfFormFieldComponent, { optional: true });
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly label = input('');
  readonly placeholder = input('');
  readonly filterable = input(false);
  readonly clearable = input(true);
  readonly required = input(false);
  readonly error = input<string | null>(null);
  readonly optionList = input.required<readonly VfSelectOption<T>[]>();
  readonly value = model<T | null>(null);

  protected readonly control = new FormControl<T | null>(null);

  // The underlying select expects a mutable array; the input stays readonly.
  protected readonly mutableOptions = computed(() => [...this.optionList()]);

  protected readonly isInvalid = computed(() =>
    this.formField ? this.formField.invalid() : !!this.error(),
  );

  private onChange: (value: T | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor() {
    effect(() => {
      const next = this.value();
      if (this.control.value !== next) {
        this.control.setValue(next, { emitEvent: false });
      }
    });

    this.control.valueChanges.pipe(takeUntilDestroyed()).subscribe((next) => {
      const normalized = next ?? null;
      this.value.set(normalized);
      this.onChange(normalized);
    });

    // The component foundation owns the combobox element and exposes no
    // invalid state, so the aria facts are stamped onto it (STD-UX-090/091).
    effect(() => {
      const invalid = this.isInvalid();
      const describedBy = this.formField?.messageId ?? null;
      const combobox = this.host.nativeElement.querySelector('[role="combobox"]');
      if (combobox) {
        combobox.setAttribute('aria-invalid', String(invalid));
        if (describedBy) {
          combobox.setAttribute('aria-describedby', describedBy);
        }
      }
    });
  }

  writeValue(next: T | null): void {
    this.value.set(next);
  }

  registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) {
      this.control.disable({ emitEvent: false });
    } else {
      this.control.enable({ emitEvent: false });
    }
  }

  protected markTouched(): void {
    this.onTouched();
  }
}
