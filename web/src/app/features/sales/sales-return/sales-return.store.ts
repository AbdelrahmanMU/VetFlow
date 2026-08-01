import { Injectable, inject, signal } from '@angular/core';

import { ApiError } from '../../../core/api/problem-details';
import {
  ApiErrorMapper,
  ClassifiedFailure,
  FailureMessageOverrides,
} from '../../../core/validation/api-error-mapper';
import { SalesReturnApiService } from './sales-return-api.service';
import { ReturnableSaleLine } from './sales-return.models';

/**
 * The screen's ruled contextual wordings (sales ui.md §المرتجع;
 * BR-SAL-015..018, BR-INV-061/068) — overrides on the shared
 * ValidationRegistry defaults, never a fork of it (STD-UX-110/111).
 */
const SALES_RETURN_MESSAGES: FailureMessageOverrides = {
  'VTF-SAL-015': 'salesReturn.error.invoiceNotCommitted',
  'VTF-SAL-016': 'salesReturn.error.exceedsReturnable',
  'VTF-SAL-017': 'salesReturn.error.lineComposition',
  'VTF-SAL-018': 'salesReturn.error.notDraft',
  'VTF-SAL-019': 'salesReturn.error.noLines',
  'VTF-SAL-020': 'salesReturn.error.traceUnusable',
  'VTF-INV-061': 'salesReturn.error.belowZero',
  'VTF-INV-068': 'salesReturn.error.conflict',
  notFound: 'salesReturn.error.notFound',
  system: 'salesReturn.error.unknown',
};

export type SalesReturnSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly number: string }
  | {
      readonly kind: 'failed';
      readonly failure: ClassifiedFailure;
      /**
       * True when the draft document was created before the sequence failed —
       * the page states the partial document state explicitly (STD-UX-042):
       * a draft exists, nothing is committed, no stock moved (BR-SAL-018).
       */
      readonly draftCreated: boolean;
    };

/**
 * Sales-return state (REQ-SAL-004): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013).
 *
 * The store holds no copy of a rule and decides nothing: the returnable ceiling is the server's
 * number (BR-SAL-016) and is only *displayed* here, and which batches the quantity goes back to is
 * never known on this side at all (BR-SAL-013, BR-SAL-017). Client-side quantity checks exist to
 * spare a round trip, never to be the authority. Every failure passes through the shared
 * ApiErrorMapper (STD-UX-123).
 */
@Injectable()
export class SalesReturnStore {
  private readonly api = inject(SalesReturnApiService);
  private readonly mapper = inject(ApiErrorMapper);

  readonly lines = signal<readonly ReturnableSaleLine[]>([]);
  readonly linesLoading = signal(false);
  /** True when the invoice cannot be returned against at all (not Committed — BR-SAL-015). */
  readonly unavailable = signal(false);
  readonly submit = signal<SalesReturnSubmitState>({ kind: 'idle' });

  loadLines(invoiceId: string): void {
    this.linesLoading.set(true);
    this.unavailable.set(false);
    this.api.getReturnableLines(invoiceId).subscribe({
      next: (lines) => {
        this.lines.set(lines);
        this.linesLoading.set(false);
      },
      error: (error: unknown) => {
        this.lines.set([]);
        this.linesLoading.set(false);
        // 404 here means "no returnable screen for this invoice" — it does not exist or is still a
        // draft. Both are the same thing to the user: there is nothing to return.
        this.unavailable.set(error instanceof ApiError && error.status === 404);
      },
    });
  }

  /**
   * Create the draft, add every chosen line, then commit — the document lifecycle the owner ruled
   * (DEC-SAL-010). The commit is what moves stock, atomically across all lines and all batches
   * (BR-SAL-018), so a failure at any step leaves no partial stock effect; a failure after the
   * create leaves a draft document, which the failed state names so the page can say so
   * (STD-UX-042).
   */
  save(
    invoiceId: string,
    returnDate: string,
    notes: string | null,
    quantities: ReadonlyMap<string, number>,
  ): void {
    const chosen = [...quantities.entries()].filter(([, quantity]) => quantity > 0);
    if (chosen.length === 0) {
      // The client-side half of BR-SAL-019 (the server enforces it at commit):
      // a synthetic business failure with the ruled wording, same shape as a
      // classified one so the page has a single rendering path.
      this.submit.set({
        kind: 'failed',
        failure: {
          kind: 'business',
          code: null,
          messageKey: 'salesReturn.error.noLines',
          retryable: false,
          fieldErrors: null,
        },
        draftCreated: false,
      });
      return;
    }

    this.submit.set({ kind: 'saving' });

    this.api.createReturn({ salesInvoiceId: invoiceId, returnDate, notes }).subscribe({
      next: (created) => this.addLinesThenCommit(created.id, created.number, chosen),
      error: (error: unknown) => this.fail(error, false),
    });
  }

  private addLinesThenCommit(
    returnId: string,
    number: string,
    chosen: readonly (readonly [string, number])[],
  ): void {
    const [next, ...rest] = chosen;
    if (next === undefined) {
      this.api.commit(returnId).subscribe({
        next: () => this.submit.set({ kind: 'saved', number }),
        error: (error: unknown) => this.fail(error, true),
      });
      return;
    }

    this.api.addLine(returnId, { salesLineItemId: next[0], quantity: next[1] }).subscribe({
      next: () => this.addLinesThenCommit(returnId, number, rest),
      error: (error: unknown) => this.fail(error, true),
    });
  }

  reset(): void {
    this.submit.set({ kind: 'idle' });
  }

  private fail(error: unknown, draftCreated: boolean): void {
    this.submit.set({
      kind: 'failed',
      failure: this.mapper.map(error, SALES_RETURN_MESSAGES),
      draftCreated,
    });
  }
}
