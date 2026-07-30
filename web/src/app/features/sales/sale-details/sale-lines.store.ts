import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { AddSaleLinePayload, CommitFailure, CommitRejection, SaleLine } from './sale-lines.models';
import { SaleLinesApiService } from './sale-lines-api.service';

export type SaleLinesViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly lines: readonly SaleLine[] };

/** The error codes the commit can answer with (ADR-0018); the UI branches on these, never on text. */
const InsufficientStockCode = 'VTF-INV-052';
const ConcurrencyConflictCode = 'VTF-INV-056';
const InexactConversionCode = 'VTF-SAL-012';

/**
 * Sales line-items state (REQ-SAL-001/002/003): a reactive list read (mirroring the purchase-lines
 * store), plus add/remove mutations and the commit, each refreshing from the server on success —
 * no optimistic UI (STD-FE-036). The invoice total is never computed here (BR-SAL-005); the page
 * re-reads the header after a change.
 *
 * The commit's rejection is classified by error code so the page can show the right message and,
 * for a concurrency conflict, offer a retry (BR-INV-056, DEC-INV-023). A rejected commit changes
 * nothing — the invoice stays a draft with all its lines (BR-SAL-012).
 */
@Injectable()
export class SaleLinesStore {
  private readonly api = inject(SaleLinesApiService);

  private readonly invoiceId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);
  private readonly request = computed(() => ({ id: this.invoiceId(), reload: this.reloadCounter() }));

  private readonly _saving = signal(false);
  readonly saving = this._saving.asReadonly();

  /** The current lines when the read is ready (empty otherwise) — the commit action consumes them. */
  readonly lines = computed(() => {
    const view = this.view();
    return view.kind === 'ready' ? view.lines : [];
  });

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) => {
        if (!id) {
          return of<SaleLinesViewState>({ kind: 'loading' });
        }

        return this.api.getLines(id).pipe(
          map((lines): SaleLinesViewState => ({ kind: 'ready', lines })),
          startWith<SaleLinesViewState>({ kind: 'loading' }),
          catchError(() => of<SaleLinesViewState>({ kind: 'error' })),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as SaleLinesViewState },
  );

  setId(id: string): void {
    this.invoiceId.set(id);
  }

  refresh(): void {
    this.reloadCounter.update((count) => count + 1);
  }

  /** POST a line; on success refresh the list and report so the page re-reads the total. */
  add(payload: AddSaleLinePayload, done: (succeeded: boolean) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.addLine(id, payload).subscribe({
      next: () => {
        this._saving.set(false);
        this.refresh();
        done(true);
      },
      error: () => {
        this._saving.set(false);
        done(false);
      },
    });
  }

  /** DELETE a line; on success refresh the list and report so the page re-reads the total. */
  remove(lineId: string, done: (succeeded: boolean) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.removeLine(id, lineId).subscribe({
      next: () => {
        this._saving.set(false);
        this.refresh();
        done(true);
      },
      error: () => {
        this._saving.set(false);
        done(false);
      },
    });
  }

  /**
   * Commit the sale (REQ-SAL-003). On success the page re-reads the header: the invoice is now
   * Committed and immutable (BR-SAL-011). On rejection nothing changed — the caller receives the
   * classified reason so it can name the products or offer a retry.
   */
  commit(done: (rejection: CommitRejection | null) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.commit(id).subscribe({
      next: () => {
        this._saving.set(false);
        done(null);
      },
      error: (error: unknown) => {
        this._saving.set(false);
        done(classify(error));
      },
    });
  }
}

function classify(error: unknown): CommitRejection {
  if (!(error instanceof ApiError)) {
    return { failure: 'other', products: null };
  }

  const failure: CommitFailure =
    error.errorCode === InsufficientStockCode
      ? 'insufficientStock'
      : error.errorCode === ConcurrencyConflictCode
        ? 'concurrencyConflict'
        : error.errorCode === InexactConversionCode
          ? 'inexactConversion'
          : 'other';

  // Insufficient stock names every product that fell short (`products`); an inexact conversion
  // names the single offending line's product (`product`). Both are data the server produced —
  // the UI only renders what it was given (AC-SAL-009, AC-SAL-013).
  const metadata = error.problem?.metadata;
  return { failure, products: metadata?.['products'] ?? metadata?.['product'] ?? null };
}
