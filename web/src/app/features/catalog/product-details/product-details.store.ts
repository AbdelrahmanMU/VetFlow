import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { ProductDetails } from './product-details.models';
import { ProductDetailsApiService } from './product-details-api.service';

export type ProductDetailsViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | { readonly kind: 'ready'; readonly product: ProductDetails };

/**
 * Product-details state (screen S2): the four data-view states (STD-FE-030),
 * with 404 surfaced as its own not-found state (REQ-CAT-044 — history is always
 * reachable, a missing product is distinct from a transport error).
 */
@Injectable()
export class ProductDetailsStore {
  private readonly api = inject(ProductDetailsApiService);

  private readonly productId = signal<string | null>(null);
  private readonly reloadCounter = signal(0);

  private readonly request = computed(() => ({ id: this.productId(), reload: this.reloadCounter() }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) => {
        if (!id) {
          return of<ProductDetailsViewState>({ kind: 'loading' });
        }

        return this.api.getById(id).pipe(
          map((product): ProductDetailsViewState => ({ kind: 'ready', product })),
          startWith<ProductDetailsViewState>({ kind: 'loading' }),
          catchError((error: unknown) =>
            of<ProductDetailsViewState>(
              error instanceof ApiError && error.status === 404 ? { kind: 'notFound' } : { kind: 'error' },
            ),
          ),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as ProductDetailsViewState },
  );

  setId(id: string): void {
    this.productId.set(id);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
