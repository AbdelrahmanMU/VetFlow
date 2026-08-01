import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiErrorMapper, ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { AddSaleLinePayload, SaleLine } from './sale-lines.models';
import { SaleLinesApiService } from './sale-lines-api.service';

export type SaleLinesViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly lines: readonly SaleLine[] };

/**
 * Sales line-items state (REQ-SAL-001/002/003): a reactive list read (mirroring the purchase-lines
 * store), plus add/remove mutations and the commit, each refreshing from the server on success —
 * no optimistic UI (STD-FE-036). The invoice total is never computed here (BR-SAL-005); the page
 * re-reads the header after a change.
 *
 * Every failure passes through the shared ApiErrorMapper (STD-UX-123) and reaches the caller
 * classified — code, message key, metadata params, and the retryable flag (BR-INV-056,
 * DEC-INV-023). A rejected commit changes nothing — the invoice stays a draft with all its lines
 * (BR-SAL-012).
 */
@Injectable()
export class SaleLinesStore {
  private readonly api = inject(SaleLinesApiService);
  private readonly mapper = inject(ApiErrorMapper);

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

  /** POST a line; on success (`null`) refresh the list so the page re-reads the total. */
  add(payload: AddSaleLinePayload, done: (failure: ClassifiedFailure | null) => void): void {
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
        done(this.mapper.map(error, { system: 'saleDetails.lines.dialog.error' }));
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
   * Commit the sale (REQ-SAL-003). On success the page re-reads the header: the invoice is now
   * Committed and immutable (BR-SAL-011). On rejection nothing changed — the caller receives the
   * classified failure (code + metadata params + retryable) so the dialog can name the products
   * the server named (AC-SAL-009/013) or offer a retry (DEC-INV-023).
   */
  commit(done: (failure: ClassifiedFailure | null) => void): void {
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
        done(this.mapper.map(error, { system: 'saleDetails.commit.error.other' }));
      },
    });
  }
}
