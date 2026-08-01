import { Injectable } from '@angular/core';

import { ApiError } from '../api/problem-details';
import { MessageKey } from '../i18n/ar';
import { VTF_ERROR_REGISTRY } from './validation-registry';

/**
 * The ApiErrorMapper (validation-and-guidance.md §13 item 5, STD-UX-123):
 * every API failure passes through here and comes out classified into the
 * standard's categories (§2) with a translatable message. Classification is
 * by `errorCode` only (STD-UX-030, STD-FE-037) — with one documented
 * exception: a code-less 404 is classified by status until AMD-1 gives
 * entity 404s a code.
 */
export type FailureKind = 'field' | 'business' | 'concurrency' | 'notFound' | 'system';

export interface ClassifiedFailure {
  readonly kind: FailureKind;
  readonly code: string | null;
  readonly messageKey: MessageKey;
  /** Response `metadata` passed through as message params (STD-UX-034). Data, never copy (ADR-0018). */
  readonly params?: Record<string, string>;
  readonly retryable: boolean;
  /** The raw per-field dictionary of a `VTF-VAL-001` response, for projection (STD-UX-019). */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>> | null;
}

/**
 * Contextual message overrides, keyed by error code — plus the two
 * status-shaped cases `'notFound'` and `'system'`. Allowed only where a
 * module `ui.md` rules the contextual wording (STD-UX-111).
 */
export type FailureMessageOverrides = Readonly<Partial<Record<string, MessageKey>>>;

@Injectable({ providedIn: 'root' })
export class ApiErrorMapper {
  map(error: unknown, overrides?: FailureMessageOverrides): ClassifiedFailure {
    if (error instanceof ApiError) {
      const code = error.errorCode;
      const entry = code ? VTF_ERROR_REGISTRY[code] : undefined;
      if (code && entry) {
        return {
          kind: entry.kind,
          code,
          messageKey: overrides?.[code] ?? entry.messageKey,
          params: error.problem?.metadata,
          retryable: entry.retryable,
          fieldErrors: entry.kind === 'field' ? (error.problem?.errors ?? null) : null,
        };
      }

      // No code (or an uncatalogued one) and status 404: the entity-missing
      // shape. Status-based branching stands only here, until AMD-1 is ruled.
      if (error.status === 404) {
        return {
          kind: 'notFound',
          code: null,
          messageKey: overrides?.['notFound'] ?? 'errors.notFound',
          retryable: false,
          fieldErrors: null,
        };
      }
    }

    // Unknown code, network failure, or 500 — category D (STD-UX-040/115).
    return {
      kind: 'system',
      code: error instanceof ApiError ? error.errorCode : null,
      messageKey: overrides?.['system'] ?? 'errors.system',
      retryable: false,
      fieldErrors: null,
    };
  }
}
