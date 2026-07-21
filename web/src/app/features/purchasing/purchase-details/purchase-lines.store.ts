import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { AddPurchaseLinePayload, PurchaseLine } from './purchase-lines.models';
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
 */
@Injectable()
export class PurchaseLinesStore {
  private readonly api = inject(PurchaseLinesApiService);

  private readonly invoiceId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);
  private readonly request = computed(() => ({ id: this.invoiceId(), reload: this.reloadCounter() }));

  private readonly _saving = signal(false);
  readonly saving = this._saving.asReadonly();

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

  /** POST a line; on success refresh the list and report success so the page re-reads the total. */
  add(payload: AddPurchaseLinePayload, done: (succeeded: boolean) => void): void {
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
}
