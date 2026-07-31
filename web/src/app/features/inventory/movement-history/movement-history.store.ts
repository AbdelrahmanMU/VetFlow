import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { MovementHistoryItem } from './movement-history.models';
import { MovementHistoryApiService } from './movement-history-api.service';

export type MovementHistoryViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly MovementHistoryItem[]; readonly totalCount: number };

/**
 * Inventory movement history state (REQ-INV-005): signals for state, RxJS only at the HTTP
 * boundary (STD-FE-012/013); switchMap drops stale responses.
 *
 * Read-only by construction — there is no mutation action here, and no filter or sort state
 * either: the order is fixed newest-first and this slice has no filters (BR-INV-039, BR-INV-044).
 */
@Injectable()
export class MovementHistoryStore {
  static readonly PageSize = 25;

  private readonly api = inject(MovementHistoryApiService);

  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed(() => ({
    page: this.page(),
    pageSize: MovementHistoryStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getMovements(request).pipe(
          map(
            (result): MovementHistoryViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<MovementHistoryViewState>({ kind: 'loading' }),
          catchError(() => of<MovementHistoryViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as MovementHistoryViewState },
  );

  /** True only when the ledger has nothing to show at all — there are no filters to blame. */
  readonly isEmpty = computed(() => {
    const view = this.view();
    return view.kind === 'ready' && view.items.length === 0;
  });

  setPage(page: number): void {
    this.page.set(page);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
