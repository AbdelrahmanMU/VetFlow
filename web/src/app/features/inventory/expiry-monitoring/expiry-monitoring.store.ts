import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { TranslationService } from '../../../core/i18n/translation.service';
import {
  CategoryOption,
  EMPTY_EXPIRY_FILTERS,
  ExpiryMonitoringFilters,
  ExpiryMonitoringItem,
  ExpiryMonitoringRequest,
} from './expiry-monitoring.models';
import { ExpiryMonitoringApiService } from './expiry-monitoring-api.service';

export type ExpiryMonitoringViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly ExpiryMonitoringItem[]; readonly totalCount: number };

export type ExpiryEmptyKind = 'none' | 'search' | 'filters' | null;

export interface AppliedExpiryChip {
  readonly key: keyof ExpiryMonitoringFilters;
  readonly label: string;
}

/**
 * Expiry monitoring state (REQ-INV-004): signals for state, RxJS only at the HTTP boundary
 * (STD-FE-012/013); switchMap drops stale responses. Read-only, no mutation actions and no
 * user-selectable sort — the order is fixed (BR-INV-032/037).
 */
@Injectable()
export class ExpiryMonitoringStore {
  static readonly PageSize = 25;

  private readonly api = inject(ExpiryMonitoringApiService);
  private readonly t = inject(TranslationService);

  readonly search = signal('');
  readonly filters = signal<ExpiryMonitoringFilters>(EMPTY_EXPIRY_FILTERS);
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  readonly categoryOptions = toSignal(
    this.api.getCategoryOptions().pipe(
      map((result) => result.items),
      catchError(() => of<readonly CategoryOption[]>([])),
    ),
    { initialValue: [] as readonly CategoryOption[] },
  );

  private readonly request = computed<ExpiryMonitoringRequest & { readonly reload: number }>(() => ({
    search: this.search(),
    filters: this.filters(),
    page: this.page(),
    pageSize: ExpiryMonitoringStore.PageSize,
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        this.api.getExpiring(request).pipe(
          map(
            (result): ExpiryMonitoringViewState => ({
              kind: 'ready',
              items: result.items,
              totalCount: result.totalCount,
            }),
          ),
          startWith<ExpiryMonitoringViewState>({ kind: 'loading' }),
          catchError(() => of<ExpiryMonitoringViewState>({ kind: 'error' })),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as ExpiryMonitoringViewState },
  );

  readonly appliedChips = computed<readonly AppliedExpiryChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedExpiryChip[] = [];

    if (filters.category) {
      chips.push({
        key: 'category',
        label: `${this.t.t('expiry.filter.category')}: ${filters.category.name}`,
      });
    }

    if (filters.expired) {
      chips.push({ key: 'expired', label: this.t.t('expiry.filter.expired') });
    }

    if (filters.expiringSoon) {
      chips.push({ key: 'expiringSoon', label: this.t.t('expiry.filter.expiringSoon') });
    }

    return chips;
  });

  readonly emptyKind = computed<ExpiryEmptyKind>(() => {
    const view = this.view();
    if (view.kind !== 'ready' || view.items.length > 0) {
      return null;
    }

    if (this.search().trim().length > 0) {
      return 'search';
    }

    return this.appliedChips().length > 0 ? 'filters' : 'none';
  });

  setSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  setFilters(filters: ExpiryMonitoringFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof ExpiryMonitoringFilters): void {
    const reset = key === 'category' ? null : false;
    this.filters.update((filters) => ({ ...filters, [key]: reset }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_EXPIRY_FILTERS);
    this.page.set(1);
  }

  setPage(page: number): void {
    this.page.set(page);
  }

  retry(): void {
    this.reloadCounter.update((count) => count + 1);
  }
}
