import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { PurchaseDetails } from './purchase-details.models';
import { PurchaseDetailsApiService } from './purchase-details-api.service';

export type PurchaseDetailsViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | { readonly kind: 'ready'; readonly invoice: PurchaseDetails };

/**
 * Purchase-details state (REQ-PUR-002): the four data-view states (STD-FE-030),
 * with 404 surfaced as its own not-found state (AC-PUR-005 — a missing invoice is
 * distinct from a transport error). Mirrors the Catalog product-details store.
 */
@Injectable()
export class PurchaseDetailsStore {
  private readonly api = inject(PurchaseDetailsApiService);

  private readonly invoiceId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);

  private readonly request = computed(() => ({ id: this.invoiceId(), reload: this.reloadCounter() }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) => {
        if (!id) {
          return of<PurchaseDetailsViewState>({ kind: 'loading' });
        }

        return this.api.getById(id).pipe(
          map((invoice): PurchaseDetailsViewState => ({ kind: 'ready', invoice })),
          startWith<PurchaseDetailsViewState>({ kind: 'loading' }),
          catchError((error: unknown) =>
            of<PurchaseDetailsViewState>(
              error instanceof ApiError && error.status === 404 ? { kind: 'notFound' } : { kind: 'error' },
            ),
          ),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as PurchaseDetailsViewState },
  );

  setId(id: string): void {
    this.invoiceId.set(id);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
