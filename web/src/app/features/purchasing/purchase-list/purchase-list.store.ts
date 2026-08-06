import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import {
  EMPTY_FILTERS,
  PurchaseFilters,
  PurchaseListItem,
  PurchaseListRequest,
  PurchaseSort,
} from './purchase-list.models';
import { PurchasesApiService } from './purchases-api.service';

export type PurchaseListViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly PurchaseListItem[]; readonly totalCount: number };

export type EmptyKind = 'new' | 'search' | 'filters' | null;

export interface AppliedFilterChip {
  readonly key: keyof PurchaseFilters;
  readonly label: string;
}

/**
 * Purchase list state (REQ-PUR-001): signals for state, RxJS only at the HTTP
 * boundary converted back to a signal at the edge (STD-FE-012, STD-FE-013).
 * switchMap guarantees stale responses never win. The default order is the most
 * recent invoice first — invoice date descending (BR-PUR-004).
 */
@Injectable()
export class PurchaseListStore {
  static readonly PageSize = 25;

  private readonly api = inject(PurchasesApiService);
  private readonly t = inject(TranslationService);
  private readonly format = inject(FormatService);

  readonly search = signal('');
  readonly filters = signal<PurchaseFilters>(EMPTY_FILTERS);
  readonly sort = signal<PurchaseSort>({ field: 'invoiceDate', direction: 'desc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed<PurchaseListRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    filters: this.filters(),
    sort: this.sort(),
    page: this.page(),
    pageSize: PurchaseListStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getPurchases(request).pipe(
          map(
            (result): PurchaseListViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<PurchaseListViewState>({ kind: 'loading' }),
          catchError(() => of<PurchaseListViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as PurchaseListViewState },
  );

  readonly appliedChips = computed<readonly AppliedFilterChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedFilterChip[] = [];

    if (filters.status) {
      const statusLabel = this.t.t(`purchases.status.${filters.status}`);
      chips.push({ key: 'status', label: `${this.t.t('purchases.filter.status')}: ${statusLabel}` });
    }

    if (filters.dateFrom) {
      chips.push({
        key: 'dateFrom',
        label: `${this.t.t('purchases.filter.dateFrom')}: ${this.format.date(filters.dateFrom)}`,
      });
    }

    if (filters.dateTo) {
      chips.push({
        key: 'dateTo',
        label: `${this.t.t('purchases.filter.dateTo')}: ${this.format.date(filters.dateTo)}`,
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

  setFilters(filters: PurchaseFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof PurchaseFilters): void {
    this.filters.update((filters) => ({ ...filters, [key]: null }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_FILTERS);
    this.page.set(1);
  }

  /**
   * Seeds the filters from the URL on entry, so the dashboard can link straight to «drafts»
   * (DEC-DSH-006, BR-DSH-018) instead of dropping the user on an unfiltered list to re-apply
   * by hand — which would move navigation rather than reduce it.
   *
   * **Only values already inside BR-PUR-004's approved filter list are honoured**; anything
   * else is ignored rather than passed through, so a hand-edited URL can never widen a list
   * the rule declares exhaustive.
   */
  applyDeepLink(status: string | null): void {
    if (status === 'draft' || status === 'received' || status === 'cancelled') {
      this.filters.update((filters) => ({ ...filters, status }));
      this.page.set(1);
    }
  }

  setSort(sort: PurchaseSort): void {
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
