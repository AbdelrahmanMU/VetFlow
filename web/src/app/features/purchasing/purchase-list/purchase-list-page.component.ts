import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfFilterChipComponent } from '../../../shared/ui-kit/chip/vf-filter-chip.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { VfSearchInputComponent } from '../../../shared/ui-kit/input/vf-search-input.component';
import { PurchaseFiltersDrawerComponent } from './components/purchase-filters-drawer.component';
import { PurchaseCardsComponent } from './components/purchase-cards.component';
import { PurchaseListSkeletonComponent } from './components/purchase-list-skeleton.component';
import { PurchaseTableComponent } from './components/purchase-table.component';
import { PurchaseListStore } from './purchase-list.store';
import { PurchasesApiService } from './purchases-api.service';

/**
 * قائمة المشتريات (purchasing ui.md): search first, the two documented filters
 * behind one button with removable chips, a premium RTL table with the four
 * mandatory states, and an adaptive mobile card list — mirroring the approved
 * Catalog product-list pattern. Read-only; creation arrives in Slice 3.
 */
@Component({
  selector: 'app-purchase-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchasesApiService, PurchaseListStore],
  imports: [
    VfSearchInputComponent,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfFilterChipComponent,
    VfPaginationComponent,
    PurchaseFiltersDrawerComponent,
    PurchaseCardsComponent,
    PurchaseListSkeletonComponent,
    PurchaseTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('purchases.title') }}</h1>
        <vf-button variant="primary" icon="pi-plus" (pressed)="goToCreate()">
          {{ t.t('purchases.create') }}
        </vf-button>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('purchases.search.placeholder')"
          [clearLabel]="t.t('purchases.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
        <vf-button icon="pi-sliders-h" (pressed)="filtersOpen.set(true)">
          {{ t.t('purchases.filters.open') }}
          @if (store.appliedChips().length > 0) {
            <span class="filter-count vf-num">{{ store.appliedChips().length }}</span>
          }
        </vf-button>
      </div>

      @if (store.appliedChips().length > 0) {
        <div class="chips">
          @for (chip of store.appliedChips(); track chip.key) {
            <vf-filter-chip [label]="chip.label" (removed)="store.removeFilter(chip.key)" />
          }
          <vf-button variant="quiet" (pressed)="store.clearFilters()">
            {{ t.t('purchases.filters.clearAll') }}
          </vf-button>
        </div>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-purchase-list-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('purchases.error.title')"
              [body]="t.t('purchases.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('purchases.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('new') {
                <vf-empty-state
                  icon="pi-file"
                  [title]="t.t('purchases.empty.new.title')"
                  [body]="t.t('purchases.empty.new.body')"
                >
                  <vf-button variant="primary" icon="pi-plus" (pressed)="goToCreate()">
                    {{ t.t('purchases.empty.new.action') }}
                  </vf-button>
                </vf-empty-state>
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('purchases.empty.search.title', { query: store.search() })"
                  [body]="t.t('purchases.empty.search.body')"
                />
              }
              @case ('filters') {
                <vf-empty-state
                  icon="pi-filter"
                  [title]="t.t('purchases.empty.filters.title')"
                  [body]="t.t('purchases.empty.filters.body')"
                >
                  <vf-button variant="secondary" icon="pi-filter-slash" (pressed)="store.clearFilters()">
                    {{ t.t('purchases.filters.clearAll') }}
                  </vf-button>
                </vf-empty-state>
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-purchase-cards [rows]="view.items" (open)="goToDetails($event)" />
                  } @else {
                    <app-purchase-table
                      [rows]="view.items"
                      [sort]="store.sort()"
                      (sortChange)="store.setSort($event)"
                      (open)="goToDetails($event)"
                    />
                  }
                  <vf-pagination
                    [page]="store.page()"
                    [pageSize]="pageSize"
                    [totalCount]="view.totalCount"
                    (pageChange)="store.setPage($event)"
                  />
                }
              }
            }
          }
        }
      </section>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>

      <app-purchase-filters-drawer
        [(visible)]="filtersOpen"
        [filters]="store.filters()"
        (filtersChange)="store.setFilters($event)"
        (cleared)="store.clearFilters()"
      />
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      block-size: 100dvh;
      max-inline-size: var(--vf-content-max-width);
      inline-size: 100%;
      margin-inline: auto;
      padding: var(--vf-space-5) var(--vf-space-6);
      gap: var(--vf-space-4);
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .toolbar {
      display: flex;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .toolbar-search {
      flex: 1;
      max-inline-size: 34rem;
    }

    .filter-count {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-inline-size: 1.25rem;
      block-size: 1.25rem;
      padding-inline: 0.25rem;
      border-radius: 999px;
      background: var(--vf-primary);
      color: #fff;
      font-size: var(--vf-text-caption);
      font-weight: 600;
    }

    .chips {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: var(--vf-space-2);
    }

    .list-area {
      flex: 1;
      min-block-size: 0;
      display: flex;
      flex-direction: column;
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      overflow: hidden;
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }

      .list-area {
        background: transparent;
        border: none;
      }
    }
  `,
})
export class PurchaseListPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(PurchaseListStore);

  constructor() {
    // Read once, from the entry snapshot: a deep link seeds the filters, it does not keep
    // overriding what the user does next (DEC-DSH-006).
    this.store.applyDeepLink(inject(ActivatedRoute).snapshot.queryParamMap.get('status'));
  }
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);

  protected readonly pageSize = PurchaseListStore.PageSize;
  protected readonly filtersOpen = signal(false);

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  protected goToDetails(id: string): void {
    void this.router.navigate(['/purchases', id]);
  }

  protected goToCreate(): void {
    void this.router.navigate(['/purchases/new']);
  }

  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('purchases.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('purchases.error.title');
    }

    return view.totalCount === 0
      ? this.t.t('pagination.zero')
      : this.t.t('pagination.range', {
          from: this.format.integer((this.store.page() - 1) * this.pageSize + 1),
          to: this.format.integer(Math.min(this.store.page() * this.pageSize, view.totalCount)),
          total: this.format.integer(view.totalCount),
        });
  });
}
