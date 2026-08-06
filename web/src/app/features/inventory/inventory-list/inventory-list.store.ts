import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { TranslationService } from '../../../core/i18n/translation.service';
import {
  CategoryOption,
  EMPTY_FILTERS,
  InventoryFilters,
  InventoryItem,
  InventoryListRequest,
  InventorySort,
} from './inventory-list.models';
import { InventoryApiService } from './inventory-api.service';

export type InventoryListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly InventoryItem[]; readonly totalCount: number };

export type EmptyKind = 'new' | 'search' | 'filters' | null;

export interface AppliedFilterChip {
  readonly key: keyof InventoryFilters;
  readonly label: string;
}

/**
 * Inventory projection state (REQ-INV-002): signals for state, RxJS only at the
 * HTTP boundary converted back to a signal at the edge (STD-FE-012, STD-FE-013).
 * switchMap guarantees stale responses never win. Read-only — no mutation actions
 * (BR-INV-006). The default order is the product name ascending.
 */
@Injectable()
export class InventoryListStore {
  static readonly PageSize = 25;

  private readonly api = inject(InventoryApiService);
  private readonly t = inject(TranslationService);

  readonly search = signal('');
  readonly filters = signal<InventoryFilters>(EMPTY_FILTERS);
  readonly sort = signal<InventorySort>({ field: 'product', direction: 'asc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  readonly categoryOptions = toSignal(
    this.api.getCategoryOptions().pipe(
      map((result) => result.items),
      catchError(() => of<readonly CategoryOption[]>([])),
    ),
    { initialValue: [] as readonly CategoryOption[] },
  );

  private readonly request = computed<InventoryListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    filters: this.filters(),
    sort: this.sort(),
    page: this.page(),
    pageSize: InventoryListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getInventory(request).pipe(
          map(
            (result): InventoryListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<InventoryListViewState>({ kind: 'loading' }),
          catchError(() => of<InventoryListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as InventoryListViewState },
  );

  readonly appliedChips = computed<readonly AppliedFilterChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedFilterChip[] = [];

    if (filters.category) {
      chips.push({
        key: 'category',
        label: `${this.t.t('inventory.filter.category')}: ${filters.category.name}`,
      });
    }

    if (filters.expiringSoon) {
      chips.push({ key: 'expiringSoon', label: this.t.t('inventory.filter.expiringSoon') });
    }

    if (filters.outOfStock) {
      chips.push({ key: 'outOfStock', label: this.t.t('inventory.filter.outOfStock') });
    }

    return chips;
  });

  readonly emptyKind = computed<EmptyKind>(() => {
    const view = this.view();
    if (view.kind !== 'ready' || view.items.length > 0) {
      return null;
    }

    if (this.search().trim().length > 0) {
      return 'search';
    }

    return this.appliedChips().length > 0 ? 'filters' : 'new';
  });

  setSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  setFilters(filters: InventoryFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof InventoryFilters): void {
    const reset = key === 'category' ? null : false;
    this.filters.update((filters) => ({ ...filters, [key]: reset }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_FILTERS);
    this.page.set(1);
  }

  /**
   * Seeds the filters from the URL on entry, so the dashboard's «نفد مخزونها» tile lands on
   * exactly the rows it counted (DEC-DSH-006, BR-DSH-018) rather than on the whole list.
   *
   * **Only the toggles already inside BR-INV-014's «حصرًا» list are honoured.** That rule is
   * why the dashboard has no low-stock tile at all (DEC-INV-004, DEC-DSH-005) — and nothing
   * here may quietly add one through a URL.
   */
  applyDeepLink(params: { readonly outOfStock: string | null; readonly expiringSoon: string | null }): void {
    const outOfStock = params.outOfStock === 'true';
    const expiringSoon = params.expiringSoon === 'true';
    if (!outOfStock && !expiringSoon) {
      return;
    }

    this.filters.update((filters) => ({
      ...filters,
      outOfStock: outOfStock || filters.outOfStock,
      expiringSoon: expiringSoon || filters.expiringSoon,
    }));
    this.page.set(1);
  }

  setSort(sort: InventorySort): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  setPage(page: number): void {
    this.page.set(page);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
