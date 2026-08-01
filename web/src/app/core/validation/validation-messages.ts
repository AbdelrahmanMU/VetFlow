import { ValidationErrors } from '@angular/forms';

import { MessageKey } from '../i18n/ar';

/**
 * Message resolution for field validation (validation-and-guidance.md §3):
 * one rule, one sentence (STD-UX-017). The resolver turns a control's
 * `ValidationErrors` into a single translatable message — the first error in
 * declaration order wins, a projected server error always wins (STD-UX-019).
 */
export interface ResolvedValidationMessage {
  readonly key: MessageKey;
  readonly params?: Record<string, string | number>;
}

/**
 * Per-rule message overrides. Allowed only where a module `ui.md` rules a
 * contextual wording (STD-UX-111) — e.g. adjustment's «اختر المنتج.» for its
 * product field's `required`.
 */
export type RuleMessageOverrides = Readonly<Partial<Record<string, MessageKey>>>;

/** The shared defaults for the rule shapes `vfValidators` produce. */
const DEFAULT_RULE_KEYS: Readonly<Record<string, MessageKey>> = {
  required: 'validation.required',
  maxlength: 'validation.maxLength',
  positive: 'validation.positive',
  wholeNumber: 'validation.wholeNumber',
};

function paramsFor(rule: string, detail: unknown): Record<string, string | number> | undefined {
  if (
    rule === 'maxlength' &&
    typeof detail === 'object' &&
    detail !== null &&
    'requiredLength' in detail
  ) {
    return { max: (detail as { requiredLength: number }).requiredLength };
  }

  return undefined;
}

export function resolveValidationMessage(
  errors: ValidationErrors | null,
  overrides?: RuleMessageOverrides,
): ResolvedValidationMessage | null {
  if (!errors) {
    return null;
  }

  // A projected server error (projectServerFieldErrors) carries its message
  // key as the error value and always wins — the server already decided.
  const server = errors['server'];
  if (typeof server === 'string') {
    return { key: server as MessageKey };
  }

  const rules = Object.keys(errors).filter((rule) => rule !== 'server');
  for (const rule of rules) {
    const override = overrides?.[rule];
    if (override) {
      return { key: override, params: paramsFor(rule, errors[rule]) };
    }
  }

  for (const rule of rules) {
    const fallback = DEFAULT_RULE_KEYS[rule];
    if (fallback) {
      return { key: fallback, params: paramsFor(rule, errors[rule]) };
    }
  }

  // A rule without any mapped copy is a defect (STD-UX-017); the generic
  // sentence keeps the user guided instead of blank.
  return { key: 'validation.invalid' };
}
