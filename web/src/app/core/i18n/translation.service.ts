import { Injectable } from '@angular/core';

import { AR, MessageKey } from './ar';

/**
 * The localization service (STD-FE-040): all copy is resolved through it.
 * Arabic is the MVP language; adding a language later swaps the resource,
 * not the callers (ADR-0007).
 */
@Injectable({ providedIn: 'root' })
export class TranslationService {
  t(key: MessageKey, params?: Record<string, string | number>): string {
    let message: string = AR[key];
    if (params) {
      for (const [name, value] of Object.entries(params)) {
        message = message.replace(`{${name}}`, String(value));
      }
    }

    return message;
  }
}
