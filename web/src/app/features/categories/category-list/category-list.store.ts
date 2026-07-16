import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { CategoriesApiService } from './categories-api.service';
import { CategoryListItem, CategoryListRequest, CategorySort } from './category-list.models';

export type CategoryListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly CategoryListItem[]; readonly totalCount: number };

export type CategoryEmptyKind = 'new' | 'search' | null;

/**
 * Category management list state (screen: التصنيفات, REQ-CTG-001): signals for
 * state, RxJS only at the HTTP boundary converted back to a signal at the edge
 * (STD-FE-012, STD-FE-013). switchMap guarantees stale responses never win, and
 * a reload counter re-fetches from the server after a mutation (never optimistic —
 * STD-FE-036).
 */
@Injectable()
export class CategoryListStore {
  static readonly PageSize = 25;

  private readonly api = inject(CategoriesApiService);

  readonly search = signal('');
  readonly sort = signal<CategorySort>({ field: 'name', direction: 'asc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed<CategoryListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    sort: this.sort(),
    page: this.page(),
    pageSize: CategoryListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.list(request).pipe(
          map(
            (result): CategoryListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<CategoryListViewState>({ kind: 'loading' }),
          catchError(() => of<CategoryListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as CategoryListViewState },
  );

  readonly emptyKind = computed<CategoryEmptyKind>(() => {
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

  setSort(sort: CategorySort): void {
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
