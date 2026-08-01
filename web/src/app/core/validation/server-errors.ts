import { AbstractControl } from '@angular/forms';

import { MessageKey } from '../i18n/ar';

/**
 * Projects a `VTF-VAL-001` response's `errors` dictionary back onto the
 * matching form controls (validation-and-guidance.md STD-UX-019/124): each
 * matched field shows the mapped local message in its own inline surface —
 * never only a banner (STD-UX-020). Unmatched keys are returned so the
 * caller can surface them at form level — no key is ever dropped.
 *
 * The message is a local catalog key, not the server's text (STD-UX-112);
 * the mapping from field name to key is the caller's, because what a field
 * error *means* («يوجد تصنيف بهذا الاسم بالفعل») is ruled per module.
 * `vf-form-field` clears a projected error on the next edit (STD-UX-015).
 */
export function projectServerFieldErrors(
  container: { get(path: string): AbstractControl | null },
  fieldErrors: Readonly<Record<string, readonly string[]>>,
  fieldMessages: Readonly<Partial<Record<string, MessageKey>>>,
): readonly string[] {
  const unmatched: string[] = [];
  for (const field of Object.keys(fieldErrors)) {
    const control = container.get(field);
    const messageKey = fieldMessages[field];
    if (control && messageKey) {
      control.setErrors({ ...(control.errors ?? {}), server: messageKey });
      // The server spoke at submit — the field displays immediately (moment 3).
      control.markAsTouched();
    } else {
      unmatched.push(field);
    }
  }

  return unmatched;
}
