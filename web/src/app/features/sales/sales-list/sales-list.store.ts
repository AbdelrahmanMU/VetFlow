import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import {
  EMPTY_FILTERS,
  SalesListFilters,
  SalesListItem,
  SalesListRequest,
  SalesListSort,
} from './sales-list.models';
import { SalesListApiService } from './sales-list-api.service';

export type SalesListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly SalesListItem[]; readonly totalCount: number };

export type EmptyKind = 'new' | 'search' | 'filters' | null;

export interface AppliedFilterChip {
  readonly key: keyof SalesListFilters;
  readonly label: string;
}

/**
 * Sales list state (REQ-SAL-005): signals for state, RxJS only at the HTTP
 * boundary converted back to a signal at the edge (STD-FE-012, STD-FE-013).
 * switchMap guarantees stale responses never win. The default order is the most
 * recent sale first — sale date descending (BR-SAL-019).
 */
@Injectable()
export class SalesListStore {
  static readonly PageSize = 25;

  private readonly api = inject(SalesListApiService);
  private readonly t = inject(TranslationService);
  private readonly format = inject(FormatService);

  readonly search = signal('');
  readonly filters = signal<SalesListFilters>(EMPTY_FILTERS);
  readonly sort = signal<SalesListSort>({ field: 'saleDate', direction: 'desc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed<SalesListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    filters: this.filters(),
    sort: this.sort(),
    page: this.page(),
    pageSize: SalesListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getSales(request).pipe(
          map(
            (result): SalesListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<SalesListViewState>({ kind: 'loading' }),
          catchError(() => of<SalesListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as SalesListViewState },
  );

  readonly appliedChips = computed<readonly AppliedFilterChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedFilterChip[] = [];

    if (filters.status) {
      const statusLabel = this.t.t(`sales.status.${filters.status}`);
      chips.push({ key: 'status', label: `${this.t.t('salesList.filter.status')}: ${statusLabel}` });
    }

    if (filters.dateFrom) {
      chips.push({
        key: 'dateFrom',
        label: `${this.t.t('salesList.filter.dateFrom')}: ${this.format.date(filters.dateFrom)}`,
      });
    }

    if (filters.dateTo) {
      chips.push({
        key: 'dateTo',
        label: `${this.t.t('salesList.filter.dateTo')}: ${this.format.date(filters.dateTo)}`,
      });
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

  setFilters(filters: SalesListFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof SalesListFilters): void {
    this.filters.update((filters) => ({ ...filters, [key]: null }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_FILTERS);
    this.page.set(1);
  }

  setSort(sort: SalesListSort): void {
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
