import { Injectable, inject, signal } from '@angular/core';

import { ApiError } from '../../../core/api/problem-details';
import { PurchaseReturnApiService } from './purchase-return-api.service';
import { PurchaseReturnFailure, ReturnableLine } from './purchase-return.models';

/** Error codes the return path can return — branch on the code, never on message text (STD-FE-037). */
const InvoiceNotReceivedCode = 'VTF-PUR-015';
const ExceedsReturnableCode = 'VTF-PUR-016';
const NotDraftCode = 'VTF-PUR-018';
const NoLinesCode = 'VTF-PUR-019';
const BelowZeroCode = 'VTF-INV-061';
const ConflictCode = 'VTF-INV-068';

export type PurchaseReturnSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly number: string }
  | { readonly kind: 'failed'; readonly failure: PurchaseReturnFailure };

/**
 * Purchase-return state (REQ-PUR-006): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013).
 *
 * The store holds no copy of a rule and decides nothing: the returnable ceiling is the server's
 * number (BR-PUR-016) and is only *displayed* here. Client-side quantity checks exist to spare a
 * round trip, never to be the authority — the command re-checks against the committed returns,
 * which a screen cannot see.
 */
@Injectable()
export class PurchaseReturnStore {
  private readonly api = inject(PurchaseReturnApiService);

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
   * failure at any step leaves no partial stock effect.
   */
  save(
    invoiceId: string,
    returnDate: string,
    notes: string | null,
    quantities: ReadonlyMap<string, number>,
  ): void {
    const chosen = [...quantities.entries()].filter(([, quantity]) => quantity > 0);
    if (chosen.length === 0) {
      this.submit.set({ kind: 'failed', failure: 'noLines' });
      return;
    }

    this.submit.set({ kind: 'saving' });

    this.api.createReturn({ purchaseInvoiceId: invoiceId, returnDate, notes }).subscribe({
      next: (created) => this.addLinesThenCommit(created.id, created.number, chosen),
      error: (error: unknown) => this.fail(error),
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
        error: (error: unknown) => this.fail(error),
      });
      return;
    }

    this.api.addLine(returnId, { purchaseLineItemId: next[0], quantity: next[1] }).subscribe({
      next: () => this.addLinesThenCommit(returnId, number, rest),
      error: (error: unknown) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.submit.set({ kind: 'failed', failure: PurchaseReturnStore.classify(error) });
  }

  private static classify(error: unknown): PurchaseReturnFailure {
    if (!(error instanceof ApiError)) {
      return 'unknown';
    }

    switch (error.errorCode) {
      case InvoiceNotReceivedCode:
        return 'invoiceNotReceived';
      case ExceedsReturnableCode:
        return 'exceedsReturnable';
      case NotDraftCode:
        return 'notDraft';
      case NoLinesCode:
        return 'noLines';
      case BelowZeroCode:
        return 'belowZero';
      case ConflictCode:
        return 'conflict';
      default:
        return error.status === 404 ? 'notFound' : 'unknown';
    }
  }
}
