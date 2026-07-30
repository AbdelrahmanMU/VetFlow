import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { SaleDetails } from './sale-details.models';
import { SaleDetailsApiService } from './sale-details-api.service';

export type SaleDetailsViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | { readonly kind: 'ready'; readonly invoice: SaleDetails };

/**
 * Sale-details state (REQ-SAL-002): the four data-view states (STD-FE-030), with 404 surfaced as
 * its own not-found state (AC-SAL-006 — a missing invoice is distinct from a transport error).
 * A literal mirror of the purchase-details store.
 */
@Injectable()
export class SaleDetailsStore {
  private readonly api = inject(SaleDetailsApiService);

  private readonly invoiceId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);

  private readonly request = computed(() => ({ id: this.invoiceId(), reload: this.reloadCounter() }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) => {
        if (!id) {
          return of<SaleDetailsViewState>({ kind: 'loading' });
        }

        return this.api.getById(id).pipe(
          map((invoice): SaleDetailsViewState => ({ kind: 'ready', invoice })),
          startWith<SaleDetailsViewState>({ kind: 'loading' }),
          catchError((error: unknown) =>
            of<SaleDetailsViewState>(
              error instanceof ApiError && error.status === 404 ? { kind: 'notFound' } : { kind: 'error' },
            ),
          ),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as SaleDetailsViewState },
  );

  setId(id: string): void {
    this.invoiceId.set(id);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
