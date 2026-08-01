import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfFilterChipComponent } from '../../../shared/ui-kit/chip/vf-filter-chip.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { BatchViewerCardsComponent } from './components/batch-viewer-cards.component';
import { BatchViewerFiltersDrawerComponent } from './components/batch-viewer-filters-drawer.component';
import { BatchViewerSkeletonComponent } from './components/batch-viewer-skeleton.component';
import { BatchViewerTableComponent } from './components/batch-viewer-table.component';
import { BatchViewerApiService } from './batch-viewer-api.service';
import { BatchViewerStore } from './batch-viewer.store';

/**
 * عارض الدفعات (batch viewer ui.md, REQ-INV-003): a read-only per-product batch detail
 * opened from an inventory row. A header (product + stock unit), the three documented
 * filters with removable chips, a premium RTL table with the five states, and an adaptive
 * mobile card list — mirroring the approved list pattern. Read-only (BR-INV-018); the
 * purchase reference links to the owning invoice (BR-INV-024), and Back returns to the
 * inventory projection (BR-INV-029).
 */
@Component({
  selector: 'app-batch-viewer-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [BatchViewerApiService, BatchViewerStore],
  imports: [
    RouterLink,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfFilterChipComponent,
    VfPaginationComponent,
    BatchViewerFiltersDrawerComponent,
    BatchViewerCardsComponent,
    BatchViewerSkeletonComponent,
    BatchViewerTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <div class="heading">
          <a class="back" routerLink="/inventory" [attr.aria-label]="t.t('batchViewer.back')">
            <i class="pi pi-arrow-right" aria-hidden="true"></i>
          </a>
          <div>
            <h1 class="page-title">{{ headerTitle() }}</h1>
            @if (readyView(); as view) {
              <p class="subtitle">{{ t.t('batchViewer.subtitle', { unit: view.stockUnitName }) }}</p>
            }
          </div>
        </div>
        @if (readyView()) {
          <vf-button icon="pi-sliders-h" (pressed)="filtersOpen.set(true)">
            {{ t.t('batchViewer.filters.open') }}
            @if (store.appliedChips().length > 0) {
              <span class="filter-count vf-num">{{ store.appliedChips().length }}</span>
            }
          </vf-button>
        }
      </header>

      @if (store.appliedChips().length > 0) {
        <div class="chips">
          @for (chip of store.appliedChips(); track chip.key) {
            <vf-filter-chip [label]="chip.label" (removed)="store.removeFilter(chip.key)" />
          }
          <vf-button variant="quiet" (pressed)="store.clearFilters()">
            {{ t.t('batchViewer.filters.clearAll') }}
          </vf-button>
        </div>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-batch-viewer-skeleton />
          }
          @case ('notFound') {
            <vf-empty-state
              icon="pi-inbox"
              [title]="t.t('batchViewer.notFound.title')"
              [body]="t.t('batchViewer.notFound.body')"
            >
              <vf-button variant="secondary" icon="pi-arrow-right" (pressed)="goToInventory()">
                {{ t.t('batchViewer.back') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('batchViewer.error.title')"
              [body]="t.t('batchViewer.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('batchViewer.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('none') {
                <vf-empty-state
                  icon="pi-box"
                  [title]="t.t('batchViewer.empty.none.title')"
                  [body]="t.t('batchViewer.empty.none.body')"
                />
              }
              @case ('filters') {
                <vf-empty-state
                  icon="pi-filter"
                  [title]="t.t('batchViewer.empty.filters.title')"
                  [body]="t.t('batchViewer.empty.filters.body')"
                >
                  <vf-button variant="secondary" icon="pi-filter-slash" (pressed)="store.clearFilters()">
                    {{ t.t('batchViewer.filters.clearAll') }}
                  </vf-button>
                </vf-empty-state>
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-batch-viewer-cards [rows]="view.items" />
                  } @else {
                    <app-batch-viewer-table
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

      <app-batch-viewer-filters-drawer
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

    .heading {
      display: flex;
      align-items: center;
      gap: var(--vf-space-3);
      min-inline-size: 0;
    }

    .back {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      inline-size: 2.25rem;
      block-size: 2.25rem;
      border-radius: var(--vf-radius);
      color: var(--vf-text-secondary);
      border: 1px solid var(--vf-border);
    }

    .back:focus-visible {
      outline: none;
      box-shadow: var(--vf-focus-ring);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .subtitle {
      margin: 0;
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
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
export class BatchViewerPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(BatchViewerStore);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly pageSize = BatchViewerStore.PageSize;
  protected readonly filtersOpen = signal(false);

  private readonly productId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('productId'))),
    { initialValue: null },
  );

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  protected readonly headerTitle = computed(() => {
    const view = this.readyView();
    return view ? view.productName : this.t.t('batchViewer.title');
  });

  // Screen-level load outcomes for the polite live region (STD-UX-092) — the
  // same loading / error / range announcement the list pages carry.
  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('batchViewer.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('batchViewer.error.title');
    }

    if (view.kind === 'notFound') {
      return this.t.t('batchViewer.notFound.title');
    }

    return view.totalCount === 0
      ? this.t.t('pagination.zero')
      : this.t.t('pagination.range', {
          from: this.format.integer((this.store.page() - 1) * this.pageSize + 1),
          to: this.format.integer(Math.min(this.store.page() * this.pageSize, view.totalCount)),
          total: this.format.integer(view.totalCount),
        });
  });

  protected goToInventory(): void {
    void this.router.navigate(['/inventory']);
  }

  constructor() {
    effect(() => {
      const id = this.productId();
      if (id) {
        this.store.setProductId(id);
      }
    });
  }
}
