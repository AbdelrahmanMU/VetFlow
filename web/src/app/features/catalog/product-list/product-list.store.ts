import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { TranslationService } from '../../../core/i18n/translation.service';
import {
  EMPTY_FILTERS,
  LookupOption,
  ProductFilters,
  ProductListItem,
  ProductListRequest,
  ProductSort,
} from './product-list.models';
import { ProductsApiService } from './products-api.service';

export type ProductListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly ProductListItem[]; readonly totalCount: number };

export type EmptyKind = 'new' | 'search' | 'filters' | null;

export interface AppliedFilterChip {
  readonly key: keyof ProductFilters;
  readonly label: string;
}

/**
 * Product list state (screen S1): signals for state, RxJS only at the HTTP
 * boundary converted back to a signal at the edge (STD-FE-012, STD-FE-013).
 * switchMap guarantees stale responses never win.
 */
@Injectable()
export class ProductListStore {
  static readonly PageSize = 25;

  private readonly api = inject(ProductsApiService);
  private readonly t = inject(TranslationService);

  readonly search = signal('');
  readonly filters = signal<ProductFilters>(EMPTY_FILTERS);
  readonly sort = signal<ProductSort>({ field: 'name', direction: 'asc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed<ProductListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    filters: this.filters(),
    sort: this.sort(),
    page: this.page(),
    pageSize: ProductListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getProducts(request).pipe(
          map(
            (result): ProductListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<ProductListViewState>({ kind: 'loading' }),
          catchError(() => of<ProductListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as ProductListViewState },
  );

  readonly categoryOptions = this.lookupSignal(() => this.api.getCategoryOptions());
  readonly manufacturerOptions = this.lookupSignal(() => this.api.getManufacturerOptions());
  readonly natureOptions = this.lookupSignal(() => this.api.getNatureOptions());

  readonly appliedChips = computed<readonly AppliedFilterChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedFilterChip[] = [];

    const lookups: readonly (readonly [keyof ProductFilters, LookupOption | null, string])[] = [
      ['category', filters.category, this.t.t('products.filter.category')],
      ['manufacturer', filters.manufacturer, this.t.t('products.filter.manufacturer')],
      ['nature', filters.nature, this.t.t('products.filter.nature')],
    ];
    for (const [key, option, label] of lookups) {
      if (option) {
        chips.push({ key, label: `${label}: ${option.name}` });
      }
    }

    if (filters.status) {
      const statusLabel =
        filters.status === 'active'
          ? this.t.t('products.filter.active')
          : this.t.t('products.filter.disabled');
      chips.push({ key: 'status', label: `${this.t.t('products.filter.status')}: ${statusLabel}` });
    }

    const booleans: readonly (readonly [keyof ProductFilters, boolean | null, string])[] = [
      ['refrigerated', filters.refrigerated, this.t.t('products.filter.refrigerated')],
      ['hasExpiration', filters.hasExpiration, this.t.t('products.filter.hasExpiration')],
      ['splittable', filters.splittable, this.t.t('products.filter.splittable')],
      ['hasSellingPrice', filters.hasSellingPrice, this.t.t('products.filter.hasSellingPrice')],
    ];
    for (const [key, value, label] of booleans) {
      if (value !== null) {
        const valueLabel = value ? this.t.t('products.filter.yes') : this.t.t('products.filter.no');
        chips.push({ key, label: `${label}: ${valueLabel}` });
      }
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

  setFilters(filters: ProductFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof ProductFilters): void {
    this.filters.update((filters) => ({ ...filters, [key]: null }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_FILTERS);
    this.page.set(1);
  }

  setSort(sort: ProductSort): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  setPage(page: number): void {
    this.page.set(page);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }

  private lookupSignal(load: () => ReturnType<ProductsApiService['getCategoryOptions']>) {
    return toSignal(
      load().pipe(
        map((result) => result.items),
        catchError(() => of<readonly LookupOption[]>([])),
      ),
      { initialValue: [] as readonly LookupOption[] },
    );
  }
}
