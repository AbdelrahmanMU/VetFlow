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
import { InventoryFiltersDrawerComponent } from './components/inventory-filters-drawer.component';
import { InventoryCardsComponent } from './components/inventory-cards.component';
import { InventoryListSkeletonComponent } from './components/inventory-list-skeleton.component';
import { InventoryTableComponent } from './components/inventory-table.component';
import { InventoryListStore } from './inventory-list.store';
import { InventoryApiService } from './inventory-api.service';

/**
 * قائمة المخزون (inventory ui.md): search first, the three documented filters
 * behind one button with removable chips, a premium RTL table with the four
 * mandatory states, and an adaptive mobile card list — mirroring the approved
 * product/purchase list pattern. Read-only (BR-INV-006); a row navigates to the
 * future Batch Viewer (BR-INV-015, DEC-INV-007) — navigation only, not designed here.
 */
@Component({
  selector: 'app-inventory-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [InventoryApiService, InventoryListStore],
  imports: [
    VfSearchInputComponent,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfFilterChipComponent,
    VfPaginationComponent,
    InventoryFiltersDrawerComponent,
    InventoryCardsComponent,
    InventoryListSkeletonComponent,
    InventoryTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('inventory.title') }}</h1>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('inventory.search.placeholder')"
          [clearLabel]="t.t('inventory.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
        <vf-button icon="pi-sliders-h" (pressed)="filtersOpen.set(true)">
          {{ t.t('inventory.filters.open') }}
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
            {{ t.t('inventory.filters.clearAll') }}
          </vf-button>
        </div>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-inventory-list-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('inventory.error.title')"
              [body]="t.t('inventory.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('inventory.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('new') {
                <vf-empty-state
                  icon="pi-box"
                  [title]="t.t('inventory.empty.new.title')"
                  [body]="t.t('inventory.empty.new.body')"
                />
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('inventory.empty.search.title', { query: store.search() })"
                  [body]="t.t('inventory.empty.search.body')"
                />
              }
              @case ('filters') {
                <vf-empty-state
                  icon="pi-filter"
                  [title]="t.t('inventory.empty.filters.title')"
                  [body]="t.t('inventory.empty.filters.body')"
                >
                  <vf-button variant="secondary" icon="pi-filter-slash" (pressed)="store.clearFilters()">
                    {{ t.t('inventory.filters.clearAll') }}
                  </vf-button>
                </vf-empty-state>
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-inventory-cards [rows]="view.items" (open)="goToBatchViewer($event)" />
                  } @else {
                    <app-inventory-table
                      [rows]="view.items"
                      [sort]="store.sort()"
                      (sortChange)="store.setSort($event)"
                      (open)="goToBatchViewer($event)"
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

      <app-inventory-filters-drawer
        [(visible)]="filtersOpen"
        [filters]="store.filters()"
        [categoryOptions]="store.categoryOptions()"
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
export class InventoryListPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(InventoryListStore);

  constructor() {
    // Read once, from the entry snapshot (DEC-DSH-006).
    const params = inject(ActivatedRoute).snapshot.queryParamMap;
    this.store.applyDeepLink({
      outOfStock: params.get('outOfStock'),
      expiringSoon: params.get('expiringSoon'),
    });
  }
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);

  protected readonly pageSize = InventoryListStore.PageSize;
  protected readonly filtersOpen = signal(false);

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  /**
   * Navigates to the future Batch Viewer for the product (BR-INV-015, DEC-INV-007).
   * The viewer itself is out of scope this slice — this wires the navigation intent
   * only; the destination route lands with the Batch module.
   */
  protected goToBatchViewer(productId: string): void {
    void this.router.navigate(['/inventory', productId]);
  }

  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('inventory.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('inventory.error.title');
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
