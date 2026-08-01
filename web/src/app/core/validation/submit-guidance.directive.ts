import {
  Directive,
  ElementRef,
  HostListener,
  Signal,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormGroup } from '@angular/forms';

import { ValidationFocusService } from './validation-focus.service';

/**
 * The SubmitGuidanceDirective (validation-and-guidance.md §13 item 3,
 * STD-UX-122): one implementation of moment 3 for every submitting form.
 * On submit it marks every control touched, and either emits `validSubmit`
 * or moves focus to the first invalid control (STD-UX-012/070) — per-screen
 * focus code is prohibited.
 *
 * `vf-form-field` instances register themselves; their labels and states
 * feed `vf-validation-summary` on qualifying long forms (STD-UX-023).
 */
export interface GuidedFieldRef {
  readonly controlId: string;
  readonly label: () => string;
  readonly invalid: Signal<boolean>;
}

@Directive({ selector: 'form[vfSubmitGuide]' })
export class SubmitGuidanceDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly focus = inject(ValidationFocusService);

  readonly formGroupInput = input.required<FormGroup>({ alias: 'vfSubmitGuide' });
  readonly validSubmit = output<void>();

  /** Moment-3 flag: fields display their errors from the first submit attempt on. */
  readonly submitted = signal(false);

  private readonly fields = signal<readonly GuidedFieldRef[]>([]);

  /** The currently invalid registered fields, in registration (DOM) order. */
  readonly invalidFields = computed(() => this.fields().filter((field) => field.invalid()));

  @HostListener('submit', ['$event'])
  onSubmit(event: Event): void {
    event.preventDefault();
    this.trigger();
  }

  /**
   * Runs moment 3. Public so dialogs whose action buttons live outside the
   * form element (the VfDialog footer slot) can trigger the same guidance.
   */
  trigger(): void {
    this.submitted.set(true);
    const form = this.formGroupInput();
    form.markAllAsTouched();
    if (form.valid) {
      this.validSubmit.emit();
      return;
    }

    // Wait one tick so the fields render their invalid state before the
    // focus query runs (STD-UX-070).
    setTimeout(() => this.focus.focusFirstInvalid(this.host.nativeElement), 0);
  }

  /** A fresh attempt cycle — e.g. a dialog reopening (fields keep their own state). */
  resetSubmitted(): void {
    this.submitted.set(false);
  }

  register(field: GuidedFieldRef): () => void {
    this.fields.update((fields) => [...fields, field]);
    return () => this.fields.update((fields) => fields.filter((existing) => existing !== field));
  }
}
