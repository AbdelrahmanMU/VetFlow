import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfFilterChipComponent } from '../../../shared/ui-kit/chip/vf-filter-chip.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { VfSearchInputComponent } from '../../../shared/ui-kit/input/vf-search-input.component';
import { ExpiryMonitoringCardsComponent } from './components/expiry-monitoring-cards.component';
import { ExpiryMonitoringFiltersDrawerComponent } from './components/expiry-monitoring-filters-drawer.component';
import { ExpiryMonitoringSkeletonComponent } from './components/expiry-monitoring-skeleton.component';
import { ExpiryMonitoringTableComponent } from './components/expiry-monitoring-table.component';
import { ExpiryMonitoringApiService } from './expiry-monitoring-api.service';
import { ExpiryMonitoringStore } from './expiry-monitoring.store';

/**
 * مراقبة انتهاء الصلاحية (expiry monitoring ui.md, REQ-INV-004): a read-only, clinic-wide
 * list of active batches with a real expiry that are expired or expiring soon. Search first,
 * the documented filters behind one button with removable chips, a premium RTL table with the
 * four states, and an adaptive mobile card list. Read-only — no alerts, no actions (BR-INV-032).
 */
@Component({
  selector: 'app-expiry-monitoring-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ExpiryMonitoringApiService, ExpiryMonitoringStore],
  imports: [
    VfSearchInputComponent,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfFilterChipComponent,
    VfPaginationComponent,
    ExpiryMonitoringFiltersDrawerComponent,
    ExpiryMonitoringCardsComponent,
    ExpiryMonitoringSkeletonComponent,
    ExpiryMonitoringTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('expiry.title') }}</h1>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('expiry.search.placeholder')"
          [clearLabel]="t.t('expiry.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
        <vf-button icon="pi-sliders-h" (pressed)="filtersOpen.set(true)">
          {{ t.t('expiry.filters.open') }}
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
            {{ t.t('expiry.filters.clearAll') }}
          </vf-button>
        </div>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-expiry-monitoring-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('expiry.error.title')"
              [body]="t.t('expiry.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('expiry.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('none') {
                <vf-empty-state
                  icon="pi-check-circle"
                  [title]="t.t('expiry.empty.none.title')"
                  [body]="t.t('expiry.empty.none.body')"
                />
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('expiry.empty.search.title', { query: store.search() })"
                  [body]="t.t('expiry.empty.search.body')"
                />
              }
              @case ('filters') {
                <vf-empty-state
                  icon="pi-filter"
                  [title]="t.t('expiry.empty.filters.title')"
                  [body]="t.t('expiry.empty.filters.body')"
                >
                  <vf-button variant="secondary" icon="pi-filter-slash" (pressed)="store.clearFilters()">
                    {{ t.t('expiry.filters.clearAll') }}
                  </vf-button>
                </vf-empty-state>
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-expiry-monitoring-cards [rows]="view.items" />
                  } @else {
                    <app-expiry-monitoring-table [rows]="view.items" />
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

      <app-expiry-monitoring-filters-drawer
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
export class ExpiryMonitoringPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(ExpiryMonitoringStore);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly pageSize = ExpiryMonitoringStore.PageSize;
  protected readonly filtersOpen = signal(false);

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  // Screen-level load outcomes for the polite live region (STD-UX-092) — the
  // same loading / error / range announcement the list pages carry.
  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('expiry.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('expiry.error.title');
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
