import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiErrorMapper, ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { AddPurchaseLinePayload, PurchaseLine, ReceivePurchaseInvoicePayload } from './purchase-lines.models';
import { PurchaseLinesApiService } from './purchase-lines-api.service';

export type PurchaseLinesViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly lines: readonly PurchaseLine[] };

/**
 * Purchase line-items state (REQ-PUR-004): a reactive list read (mirrors the
 * purchase-details store), plus add/remove mutations that refresh the list from the
 * server on success (no optimistic UI — STD-FE-036). The invoice total is never
 * computed here (BR-PUR-006, DEC-PUR-003); the page re-reads the header after a change.
 *
 * Every mutation failure passes through the shared ApiErrorMapper
 * (STD-UX-123) and reaches the caller classified — never a bare boolean that
 * throws the reason away.
 */
@Injectable()
export class PurchaseLinesStore {
  private readonly api = inject(PurchaseLinesApiService);
  private readonly mapper = inject(ApiErrorMapper);

  private readonly invoiceId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);
  private readonly request = computed(() => ({ id: this.invoiceId(), reload: this.reloadCounter() }));

  private readonly _saving = signal(false);
  readonly saving = this._saving.asReadonly();

  /** The current lines when the read is ready (empty otherwise) — the receive dialog consumes them. */
  readonly lines = computed(() => {
    const view = this.view();
    return view.kind === 'ready' ? view.lines : [];
  });

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) => {
        if (!id) {
          return of<PurchaseLinesViewState>({ kind: 'loading' });
        }

        return this.api.getLines(id).pipe(
          map((lines): PurchaseLinesViewState => ({ kind: 'ready', lines })),
          startWith<PurchaseLinesViewState>({ kind: 'loading' }),
          catchError(() => of<PurchaseLinesViewState>({ kind: 'error' })),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as PurchaseLinesViewState },
  );

  setId(id: string): void {
    this.invoiceId.set(id);
  }

  refresh(): void {
    this.reloadCounter.update((count) => count + 1);
  }

  /** POST a line; on success (`null`) refresh the list so the page re-reads the total. */
  add(payload: AddPurchaseLinePayload, done: (failure: ClassifiedFailure | null) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.addLine(id, payload).subscribe({
      next: () => {
        this._saving.set(false);
        this.refresh();
        done(null);
      },
      error: (error: unknown) => {
        this._saving.set(false);
        done(this.mapper.map(error, { system: 'purchaseDetails.lines.dialog.error' }));
      },
    });
  }

  /** DELETE a line; on success (`null`) refresh the list so the page re-reads the total. */
  remove(lineId: string, done: (failure: ClassifiedFailure | null) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.removeLine(id, lineId).subscribe({
      next: () => {
        this._saving.set(false);
        this.refresh();
        done(null);
      },
      error: (error: unknown) => {
        this._saving.set(false);
        done(this.mapper.map(error));
      },
    });
  }

  /**
   * Receive the invoice (REQ-PUR-005): on success (`null`) report so the page re-reads the header
   * (the status becomes Received and the invoice is immutable). No optimistic UI (STD-FE-036).
   */
  receive(payload: ReceivePurchaseInvoicePayload, done: (failure: ClassifiedFailure | null) => void): void {
    const id = this.invoiceId();
    if (!id) {
      return;
    }

    this._saving.set(true);
    this.api.receive(id, payload).subscribe({
      next: () => {
        this._saving.set(false);
        done(null);
      },
      error: (error: unknown) => {
        this._saving.set(false);
        done(this.mapper.map(error, { system: 'purchaseDetails.receive.dialog.error' }));
      },
    });
  }
}
