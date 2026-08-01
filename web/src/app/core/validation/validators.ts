import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/**
 * The shared validator library (validation-and-guidance.md §13 item 7,
 * STD-UX-125). Validators are generic shapes; the *rule* that makes one real
 * is cited at the call site (`BR-*`/`REQ-*`), never invented here
 * (STD-UX-021). Message copy is resolved by `resolveValidationMessage`.
 */
export const vfValidators = {
  /** Canonical required rule — message «هذا الحقل مطلوب.» (sales/ui.md, STD-UX-018). */
  required: Validators.required,

  /** Maximum length with its own sentence — never the required copy (STD-UX-017). */
  maxLength: (max: number): ValidatorFn => Validators.maxLength(max),

  /**
   * Strictly positive quantity (> 0). Empty passes — compose with `required`
   * so each rule keeps its own message.
   */
  positive: (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return typeof value === 'number' && value > 0 ? null : { positive: true };
  },

  /** Whole number — for quantities of non-splittable products (BR-SAL-004 family). */
  wholeNumber: (control: AbstractControl): ValidationErrors | null => {
    const value: unknown = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }

    return typeof value === 'number' && Number.isInteger(value) ? null : { wholeNumber: true };
  },
} as const;
