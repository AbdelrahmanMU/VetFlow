import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ManufacturersApiService } from './manufacturers-api.service';
import { ManufacturerListItem, ManufacturerListRequest, ManufacturerSort } from './manufacturer-list.models';

export type ManufacturerListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly ManufacturerListItem[]; readonly totalCount: number };

export type ManufacturerEmptyKind = 'new' | 'search' | null;

/**
 * Manufacturer management list state (screen: الشركات المصنعة, REQ-CAT-047): signals
 * for state, RxJS only at the HTTP boundary converted back to a signal at the edge
 * (STD-FE-012, STD-FE-013). switchMap guarantees stale responses never win, and a
 * reload counter re-fetches from the server after a mutation (never optimistic —
 * STD-FE-036). A deliberate mirror of the category list store.
 */
@Injectable()
export class ManufacturerListStore {
  static readonly PageSize = 25;

  private readonly api = inject(ManufacturersApiService);

  readonly search = signal('');
  readonly sort = signal<ManufacturerSort>({ field: 'name', direction: 'asc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed<ManufacturerListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    sort: this.sort(),
    page: this.page(),
    pageSize: ManufacturerListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.list(request).pipe(
          map(
            (result): ManufacturerListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<ManufacturerListViewState>({ kind: 'loading' }),
          catchError(() => of<ManufacturerListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as ManufacturerListViewState },
  );

  readonly emptyKind = computed<ManufacturerEmptyKind>(() => {
    const view = this.view();
    if (view.kind !== 'ready' || view.items.length > 0) {
      return null;
    }

    return this.search().trim().length > 0 ? 'search' : 'new';
  });

  setSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  setSort(sort: ManufacturerSort): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  setPage(page: number): void {
    this.page.set(page);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }

  /** Re-fetch the current view after a successful mutation (the server is the truth). */
  refresh(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
