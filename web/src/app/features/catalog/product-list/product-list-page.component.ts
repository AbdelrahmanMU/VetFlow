import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfFilterChipComponent } from '../../../shared/ui-kit/chip/vf-filter-chip.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { VfPopoverComponent } from '../../../shared/ui-kit/popover/vf-popover.component';
import { VfSearchInputComponent } from '../../../shared/ui-kit/input/vf-search-input.component';
import { ProductFiltersDrawerComponent } from './components/product-filters-drawer.component';
import { ProductCardsComponent } from './components/product-cards.component';
import { ProductListSkeletonComponent } from './components/product-list-skeleton.component';
import { ProductTableComponent } from './components/product-table.component';
import { PRODUCT_COLUMNS, ProductColumnPreferences } from './product-columns';
import { ProductListStore } from './product-list.store';
import { ProductsApiService } from './products-api.service';

/**
 * Screen S1 — قائمة المنتجات (catalog ui.md §3): search first, ten-minus-two
 * filters behind one button with removable chips, a premium RTL table with the
 * four mandatory states, and an adaptive mobile card list.
 */
@Component({
  selector: 'app-product-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProductsApiService, ProductListStore, ProductColumnPreferences],
  imports: [
    VfSearchInputComponent,
    VfButtonComponent,
    VfCheckboxComponent,
    VfEmptyStateComponent,
    VfFilterChipComponent,
    VfPaginationComponent,
    VfPopoverComponent,
    ProductFiltersDrawerComponent,
    ProductCardsComponent,
    ProductListSkeletonComponent,
    ProductTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('products.title') }}</h1>
        <vf-button variant="primary" icon="pi-plus" (pressed)="goToCreate()">
          {{ t.t('products.create') }}
        </vf-button>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('products.search.placeholder')"
          [clearLabel]="t.t('products.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
        <vf-button icon="pi-sliders-h" (pressed)="filtersOpen.set(true)">
          {{ t.t('products.filters.open') }}
          @if (store.appliedChips().length > 0) {
            <span class="filter-count vf-num">{{ store.appliedChips().length }}</span>
          }
        </vf-button>
        @if (!isMobile()) {
          <vf-button icon="pi-objects-column" (pressed)="columnsPanel.toggle($event)">
            {{ t.t('products.columns.open') }}
          </vf-button>
        }
      </div>

      @if (store.appliedChips().length > 0) {
        <div class="chips">
          @for (chip of store.appliedChips(); track chip.key) {
            <vf-filter-chip [label]="chip.label" (removed)="store.removeFilter(chip.key)" />
          }
          <vf-button variant="quiet" (pressed)="store.clearFilters()">
            {{ t.t('products.filters.clearAll') }}
          </vf-button>
        </div>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-product-list-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('products.error.title')"
              [body]="t.t('products.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('products.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('new') {
                <vf-empty-state
                  icon="pi-box"
                  [title]="t.t('products.empty.new.title')"
                  [body]="t.t('products.empty.new.body')"
                >
                  <vf-button variant="primary" icon="pi-plus" (pressed)="goToCreate()">
                    {{ t.t('products.empty.new.action') }}
                  </vf-button>
                </vf-empty-state>
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('products.empty.search.title', { query: store.search() })"
                  [body]="t.t('products.empty.search.body')"
                />
              }
              @case ('filters') {
                <vf-empty-state
                  icon="pi-filter"
                  [title]="t.t('products.empty.filters.title')"
                  [body]="t.t('products.empty.filters.body')"
                >
                  <vf-button variant="secondary" icon="pi-filter-slash" (pressed)="store.clearFilters()">
                    {{ t.t('products.filters.clearAll') }}
                  </vf-button>
                </vf-empty-state>
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-product-cards [rows]="view.items" />
                  } @else {
                    <app-product-table
                      [rows]="view.items"
                      [sort]="store.sort()"
                      (sortChange)="store.setSort($event)"
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

      <app-product-filters-drawer
        [(visible)]="filtersOpen"
        [filters]="store.filters()"
        [categoryOptions]="store.categoryOptions()"
        [manufacturerOptions]="store.manufacturerOptions()"
        [natureOptions]="store.natureOptions()"
        (filtersChange)="store.setFilters($event)"
        (cleared)="store.clearFilters()"
      />

      <vf-popover #columnsPanel>
        <div class="columns-menu">
          <span class="columns-title">{{ t.t('products.columns.title') }}</span>
          @for (column of hideableColumns; track column.id) {
            <vf-checkbox
              [checked]="columnPreferences.isVisible(column.id)"
              (toggled)="columnPreferences.toggle(column.id)"
            >
              {{ t.t(column.labelKey) }}
            </vf-checkbox>
          }
        </div>
      </vf-popover>
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

    .columns-menu {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      min-inline-size: 12rem;
    }

    .columns-title {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      margin-block-end: var(--vf-space-1);
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
export class ProductListPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(ProductListStore);
  protected readonly columnPreferences = inject(ProductColumnPreferences);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);

  protected readonly pageSize = ProductListStore.PageSize;
  protected readonly hideableColumns = PRODUCT_COLUMNS.filter((column) => column.hideable);
  protected readonly filtersOpen = signal(false);

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('products.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('products.error.title');
    }

    return view.totalCount === 0
      ? this.t.t('pagination.zero')
      : this.t.t('pagination.range', {
          from: this.format.integer((this.store.page() - 1) * this.pageSize + 1),
          to: this.format.integer(Math.min(this.store.page() * this.pageSize, view.totalCount)),
          total: this.format.integer(view.totalCount),
        });
  });

  protected goToCreate(): void {
    void this.router.navigate(['/catalog/products/new']);
  }
}
