import { Injectable, inject, signal } from '@angular/core';

import { ApiError } from '../../../core/api/problem-details';
import {
  ApiErrorMapper,
  ClassifiedFailure,
  FailureMessageOverrides,
} from '../../../core/validation/api-error-mapper';
import { PurchaseReturnApiService } from './purchase-return-api.service';
import { ReturnableLine } from './purchase-return.models';

/**
 * The screen's ruled contextual wordings (purchasing ui.md §المرتجع;
 * BR-PUR-015/016, BR-INV-061/068) — overrides on the shared
 * ValidationRegistry defaults, never a fork of it (STD-UX-110/111).
 */
const PURCHASE_RETURN_MESSAGES: FailureMessageOverrides = {
  'VTF-PUR-015': 'purchaseReturn.error.invoiceNotReceived',
  'VTF-PUR-016': 'purchaseReturn.error.exceedsReturnable',
  'VTF-PUR-018': 'purchaseReturn.error.notDraft',
  'VTF-PUR-019': 'purchaseReturn.error.noLines',
  'VTF-INV-061': 'purchaseReturn.error.belowZero',
  'VTF-INV-068': 'purchaseReturn.error.conflict',
  notFound: 'purchaseReturn.error.notFound',
  system: 'purchaseReturn.error.unknown',
};

export type PurchaseReturnSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly number: string }
  | {
      readonly kind: 'failed';
      readonly failure: ClassifiedFailure;
      /**
       * True when the draft document was created before the sequence failed —
       * the page states the partial document state explicitly (STD-UX-042):
       * a draft exists, nothing is committed, no stock moved (BR-PUR-018).
       */
      readonly draftCreated: boolean;
    };

/**
 * Purchase-return state (REQ-PUR-006): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013).
 *
 * The store holds no copy of a rule and decides nothing: the returnable ceiling is the server's
 * number (BR-PUR-016) and is only *displayed* here. Client-side quantity checks exist to spare a
 * round trip, never to be the authority — the command re-checks against the committed returns,
 * which a screen cannot see. Every failure passes through the shared ApiErrorMapper (STD-UX-123).
 */
@Injectable()
export class PurchaseReturnStore {
  private readonly api = inject(PurchaseReturnApiService);
  private readonly mapper = inject(ApiErrorMapper);

  readonly lines = signal<readonly ReturnableLine[]>([]);
  readonly linesLoading = signal(false);
  /** True when the invoice cannot be returned against at all (not Received — BR-PUR-015). */
  readonly unavailable = signal(false);
  readonly submit = signal<PurchaseReturnSubmitState>({ kind: 'idle' });

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
        // 404 here means "no returnable screen for this invoice" — it does not exist or is not
        // Received. Both are the same thing to the user: there is nothing to return.
        this.unavailable.set(error instanceof ApiError && error.status === 404);
      },
    });
  }

  /**
   * Create the draft, add every chosen line, then commit — the document lifecycle the owner ruled
   * (DEC-PUR-010). The commit is what moves stock, atomically across all lines (BR-PUR-018), so a
   * failure at any step leaves no partial stock effect; a failure after the create leaves a draft
   * document, which the failed state names so the page can say so (STD-UX-042).
   */
  save(
    invoiceId: string,
    returnDate: string,
    notes: string | null,
    quantities: ReadonlyMap<string, number>,
  ): void {
    const chosen = [...quantities.entries()].filter(([, quantity]) => quantity > 0);
    if (chosen.length === 0) {
      // The client-side half of BR-PUR-019 (the server enforces it at commit):
      // a synthetic business failure with the ruled wording, same shape as a
      // classified one so the page has a single rendering path.
      this.submit.set({
        kind: 'failed',
        failure: {
          kind: 'business',
          code: null,
          messageKey: 'purchaseReturn.error.noLines',
          retryable: false,
          fieldErrors: null,
        },
        draftCreated: false,
      });
      return;
    }

    this.submit.set({ kind: 'saving' });

    this.api.createReturn({ purchaseInvoiceId: invoiceId, returnDate, notes }).subscribe({
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

    this.api.addLine(returnId, { purchaseLineItemId: next[0], quantity: next[1] }).subscribe({
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
      failure: this.mapper.map(error, PURCHASE_RETURN_MESSAGES),
      draftCreated,
    });
  }
}
