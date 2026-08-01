import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { MovementHistoryCardsComponent } from './components/movement-history-cards.component';
import { MovementHistorySkeletonComponent } from './components/movement-history-skeleton.component';
import { MovementHistoryTableComponent } from './components/movement-history-table.component';
import { MovementHistoryApiService } from './movement-history-api.service';
import { MovementHistoryStore } from './movement-history.store';

/**
 * تاريخ حركة المخزون (inventory ui.md, REQ-INV-005): a read-only, clinic-wide chronological list
 * of stock movements, newest first, projected over the movement ledger (BR-INV-040 as corrected).
 *
 * <b>Read-only and immutable</b> (BR-INV-039, AC-INV-035): the screen carries no action, no
 * button that changes stock, and no edit or delete affordance anywhere — the only interactive
 * elements are pagination, the retry on an error, and the reference links out to the causing
 * document. There are no filters and no sort control in this slice (BR-INV-044), so the empty
 * state has exactly one meaning: the ledger has nothing in it yet.
 */
@Component({
  selector: 'app-movement-history-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MovementHistoryApiService, MovementHistoryStore],
  imports: [
    VfButtonComponent,
    VfEmptyStateComponent,
    VfPaginationComponent,
    MovementHistoryCardsComponent,
    MovementHistorySkeletonComponent,
    MovementHistoryTableComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('history.title') }}</h1>
        <p class="page-subtitle">{{ t.t('history.subtitle') }}</p>
      </header>

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-movement-history-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('history.error.title')"
              [body]="t.t('history.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('history.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @if (store.isEmpty()) {
              <vf-empty-state
                icon="pi-history"
                [title]="t.t('history.empty.title')"
                [body]="t.t('history.empty.body')"
              />
            } @else if (readyView(); as view) {
              @if (isMobile()) {
                <app-movement-history-cards [rows]="view.items" />
              } @else {
                <app-movement-history-table [rows]="view.items" />
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
      </section>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
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
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .page-subtitle {
      margin: 0;
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
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
export class MovementHistoryPageComponent {
  protected readonly t = inject(TranslationService);
  private readonly format = inject(FormatService);
  protected readonly store = inject(MovementHistoryStore);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly pageSize = MovementHistoryStore.PageSize;

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
      return this.t.t('history.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('history.error.title');
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
