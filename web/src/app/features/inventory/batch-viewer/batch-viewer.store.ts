import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { TranslationService } from '../../../core/i18n/translation.service';
import {
  BatchViewerFilters,
  BatchViewerItem,
  BatchViewerRequest,
  BatchViewerSort,
  EMPTY_BATCH_FILTERS,
} from './batch-viewer.models';
import { BatchViewerApiService } from './batch-viewer-api.service';

export type BatchViewerViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | {
      readonly kind: 'ready';
      readonly productName: string;
      readonly stockUnitName: string;
      readonly items: readonly BatchViewerItem[];
      readonly totalCount: number;
    };

export type BatchEmptyKind = 'none' | 'filters' | null;

export interface AppliedBatchChip {
  readonly key: keyof BatchViewerFilters;
  readonly label: string;
}

/**
 * Batch viewer state (REQ-INV-003): the whitelisted filters/sort/pagination over one
 * product's batches, with 404 surfaced as its own not-found state (AC-INV-022). Signals
 * for state, RxJS only at the HTTP boundary (STD-FE-012/013); switchMap drops stale
 * responses. Read-only — no mutation actions (BR-INV-018). The default order is the
 * receive date descending (BR-INV-031).
 */
@Injectable()
export class BatchViewerStore {
  static readonly PageSize = 25;

  private readonly api = inject(BatchViewerApiService);
  private readonly t = inject(TranslationService);

  private readonly productId = signal<string | null>(null);
  readonly filters = signal<BatchViewerFilters>(EMPTY_BATCH_FILTERS);
  readonly sort = signal<BatchViewerSort>({ field: 'receiveDate', direction: 'desc' });
  readonly page = signal(1);
  private readonly reloadCounter = signal(0);

  private readonly request = computed(() => ({
    productId: this.productId(),
    filters: this.filters(),
    sort: this.sort(),
    page: this.page(),
    reload: this.reloadCounter(),
  }));

  readonly view = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ productId, filters, sort, page }) => {
        if (!productId) {
          return of<BatchViewerViewState>({ kind: 'loading' });
        }

        const request: BatchViewerRequest = {
          productId,
          filters,
          sort,
          page,
          pageSize: BatchViewerStore.PageSize,
        };

        return this.api.getBatches(request).pipe(
          map(
            (result): BatchViewerViewState => ({
              kind: 'ready',
              productName: result.productName,
              stockUnitName: result.stockUnitName,
              items: result.batches.items,
              totalCount: result.batches.totalCount,
            }),
          ),
          startWith<BatchViewerViewState>({ kind: 'loading' }),
          catchError((error: unknown) =>
            of<BatchViewerViewState>(
              error instanceof ApiError && error.status === 404 ? { kind: 'notFound' } : { kind: 'error' },
            ),
          ),
        );
      }),
    ),
    { initialValue: { kind: 'loading' } as BatchViewerViewState },
  );

  readonly appliedChips = computed<readonly AppliedBatchChip[]>(() => {
    const filters = this.filters();
    const chips: AppliedBatchChip[] = [];

    if (filters.status) {
      chips.push({
        key: 'status',
        label: `${this.t.t('batchViewer.filter.status')}: ${this.t.t(
          filters.status === 'active' ? 'batchViewer.status.active' : 'batchViewer.status.depleted',
        )}`,
      });
    }

    if (filters.expired) {
      chips.push({ key: 'expired', label: this.t.t('batchViewer.filter.expired') });
    }

    if (filters.expiringSoon) {
      chips.push({ key: 'expiringSoon', label: this.t.t('batchViewer.filter.expiringSoon') });
    }

    return chips;
  });

  readonly emptyKind = computed<BatchEmptyKind>(() => {
    const view = this.view();
    if (view.kind !== 'ready' || view.items.length > 0) {
      return null;
    }

    return this.appliedChips().length > 0 ? 'filters' : 'none';
  });

  setProductId(productId: string): void {
    this.productId.set(productId);
  }

  setFilters(filters: BatchViewerFilters): void {
    this.filters.set(filters);
    this.page.set(1);
  }

  removeFilter(key: keyof BatchViewerFilters): void {
    const reset = key === 'status' ? null : false;
    this.filters.update((filters) => ({ ...filters, [key]: reset }));
    this.page.set(1);
  }

  clearFilters(): void {
    this.filters.set(EMPTY_BATCH_FILTERS);
    this.page.set(1);
  }

  setSort(sort: BatchViewerSort): void {
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
