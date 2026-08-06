import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { FormatService } from '../../core/i18n/format.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { VfButtonComponent } from '../../shared/ui-kit/button/vf-button.component';
import { VfCardComponent } from '../../shared/ui-kit/card/vf-card.component';
import { VfEmptyStateComponent } from '../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfSkeletonComponent } from '../../shared/ui-kit/skeleton/vf-skeleton.component';
import { VfStatTileComponent } from '../../shared/ui-kit/stat-tile/vf-stat-tile.component';
import { DashboardApiService } from './dashboard-api.service';
import { DashboardStore } from './dashboard.store';

/**
 * لوحة التشغيل — the operational dashboard (REQ-DSH-001).
 *
 * The one focus is the attention band (§15.2); everything else is deliberately quieter. The
 * primary action is **one** button — «بيع جديد» — because §15.1 allows exactly one per screen
 * and §4 says two equally prominent buttons mean the design has not decided (DEC-DSH-008).
 *
 * Every element navigates (BR-DSH-002). Nothing here edits, commits, receives or writes
 * anything at all (BR-DSH-020).
 */
@Component({
  selector: 'vf-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore, DashboardApiService],
  imports: [
    RouterLink,
    VfButtonComponent,
    VfCardComponent,
    VfEmptyStateComponent,
    VfSkeletonComponent,
    VfStatTileComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <div class="page-heading">
          <h1 class="page-title">{{ t.t('dashboard.title') }}</h1>
        </div>
        <div class="page-actions">
          <!--
            The explicit refresh BR-DSH-015 requires: the board is a snapshot taken when it
            opened and there is deliberately no polling (DEC-DSH-010), so the owner needs a
            way to ask for a fresh one. Quiet, because §15.1 allows one primary action and it
            is «بيع جديد».
          -->
          <button type="button" class="quiet-action" (click)="store.reload()">
            <i class="pi pi-refresh" aria-hidden="true"></i>
            {{ t.t('dashboard.refresh') }}
          </button>
          <!-- Secondary actions stay visibly quieter than the one primary (§4, §15.1). -->
          <a class="quiet-action" routerLink="/purchases/new">{{ t.t('dashboard.quickActions.newPurchase') }}</a>
          <a class="quiet-action" routerLink="/catalog/products/new">{{
            t.t('dashboard.quickActions.newProduct')
          }}</a>
          <vf-button variant="primary" icon="pi-plus" (pressed)="goToNewSale()">{{ t.t('dashboard.quickActions.newSale') }}</vf-button>
        </div>
      </header>

      @switch (store.viewState().kind) {
        @case ('loading') {
          <!-- Content-shaped, so nothing jumps when the data lands (§9, §13). -->
          <section class="attention" aria-busy="true">
            <h2 class="section-title">{{ t.t('dashboard.attention.title') }}</h2>
            <div class="tiles">
              @for (placeholder of skeletonTiles; track placeholder) {
                <vf-skeleton height="7rem" />
              }
            </div>
          </section>
          <div class="panels">
            <vf-skeleton height="9rem" />
            <vf-skeleton height="9rem" />
          </div>
        }

        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-triangle"
            [title]="t.t('dashboard.section.error')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="store.reload()">
              {{ t.t('dashboard.section.retry') }}
            </vf-button>
          </vf-empty-state>
        }

        @case ('ready') {
          <section class="attention">
            <h2 class="section-title">{{ t.t('dashboard.attention.title') }}</h2>

            @if (store.allClear()) {
              <!--
                Zero is the most valuable thing this board says. It is an explicit
                reassurance, not five zero tiles and not «no data» — showing good news in the
                language of emptiness inverts its meaning (BR-DSH-013, §6).
              -->
              <vf-empty-state icon="pi-check-circle" [title]="t.t('dashboard.attention.allClear')" />
            } @else {
              <div class="tiles">
                @for (item of store.attentionItems(); track item.key) {
                  @if (item.section.status === 'failed') {
                    <!--
                      A failed section is NEVER shown as zero: «no expired batches» and
                      «could not determine expired batches» are contradictory, and conflating
                      them is false reassurance inside a safety decision (BR-DSH-014).
                    -->
                    <div class="tile-failed" role="alert">
                      <span class="tile-failed-label">{{ t.t(item.labelKey) }}</span>
                      <span class="tile-failed-message">{{ t.t('dashboard.section.error') }}</span>
                      <vf-button variant="secondary" icon="pi-refresh" (pressed)="store.reload()">
                        {{ t.t('dashboard.section.retry') }}
                      </vf-button>
                    </div>
                  } @else {
                    <vf-stat-tile
                      [label]="t.t(item.labelKey)"
                      [value]="format.integer(item.section.count ?? 0)"
                      [icon]="item.icon"
                      [tone]="item.tone"
                      [routerLink]="item.routerLink"
                      [queryParams]="item.queryParams"
                      [actionLabel]="t.t('dashboard.tile.open')"
                      [ariaLabel]="t.t(item.labelKey) + ': ' + format.integer(item.section.count ?? 0)"
                    />
                  }
                }
              </div>
            }
          </section>

          <div class="panels">
            <vf-card [heading]="t.t('dashboard.todaySales.title')">
              @if (todaySales(); as sales) {
                <a class="today" [routerLink]="'/sales'" [queryParams]="todayParams()">
                  <span class="today-count vf-num">{{ format.integer(sales.count ?? 0) }}</span>
                  <span class="today-unit">{{ t.t('dashboard.todaySales.invoices') }}</span>
                  @if (sales.total; as total) {
                    <span class="today-total vf-num">{{ format.money(total.amount, total.currency) }}</span>
                  }
                </a>
              } @else {
                <div class="panel-failed" role="alert">
                  <span>{{ t.t('dashboard.section.error') }}</span>
                  <vf-button variant="secondary" icon="pi-refresh" (pressed)="store.reload()">
                    {{ t.t('dashboard.section.retry') }}
                  </vf-button>
                </div>
              }
            </vf-card>

            <vf-card [heading]="t.t('dashboard.recentMovements.title')">
              <a card-actions class="quiet-action" routerLink="/inventory/history">
                {{ t.t('dashboard.recentMovements.viewAll') }}
              </a>
              @if (movements(); as items) {
                @if (items.length === 0) {
                  <vf-empty-state icon="pi-history" [title]="t.t('dashboard.recentMovements.empty')" />
                } @else {
                  <ul class="movements">
                    @for (movement of items; track movement.movementId) {
                      <li class="movement">
                        <span class="movement-product">{{ movement.productName }}</span>
                        <span class="movement-quantity vf-num">
                          {{ format.decimal(movement.quantity) }} {{ movement.stockUnitName }}
                        </span>
                        <span class="movement-time">{{ occurredAt(movement.occurredAt) }}</span>
                      </li>
                    }
                  </ul>
                }
              } @else {
                <div class="panel-failed" role="alert">
                  <span>{{ t.t('dashboard.section.error') }}</span>
                  <vf-button variant="secondary" icon="pi-refresh" (pressed)="store.reload()">
                    {{ t.t('dashboard.section.retry') }}
                  </vf-button>
                </div>
              }
            </vf-card>
          </div>
        }
      }
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-6);
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-4);
      flex-wrap: wrap;
    }

    .page-title {
      margin: 0;
      font-size: 1.375rem;
      font-weight: 700;
    }

    .page-actions {
      display: flex;
      align-items: center;
      gap: var(--vf-space-4);
    }

    .quiet-action {
      display: inline-flex;
      align-items: center;
      gap: var(--vf-space-1);
      min-block-size: 44px;
      padding: 0;
      background: none;
      border: none;
      font-family: inherit;
      color: var(--vf-primary);
      text-decoration: none;
      font-size: 0.875rem;
      cursor: pointer;
    }

    .quiet-action:focus-visible {
      outline: 2px solid var(--vf-primary);
      outline-offset: 2px;
    }

    .quiet-action:hover {
      text-decoration: underline;
    }

    .section-title {
      margin: 0 0 var(--vf-space-3);
      font-size: 1rem;
      font-weight: 600;
    }

    /* Auto-fit, so the band is one visual sweep on desktop and stacks on a phone without a
       second layout to maintain (§5 — the device changes priority, not the design). */
    .tiles {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: var(--vf-space-4);
    }

    .panels {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--vf-space-4);
    }

    .tile-failed,
    .panel-failed {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: var(--vf-space-2);
      padding: var(--vf-space-4);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
    }

    .tile-failed-label {
      font-size: 0.875rem;
      color: var(--vf-text-muted);
    }

    .tile-failed-message {
      color: var(--vf-danger);
      font-size: 0.875rem;
    }

    .today {
      display: flex;
      align-items: baseline;
      gap: var(--vf-space-2);
      text-decoration: none;
      color: inherit;
      min-block-size: 44px;
    }

    .today:focus-visible {
      outline: 2px solid var(--vf-primary);
      outline-offset: 2px;
    }

    .today-count {
      font-size: 1.75rem;
      font-weight: 700;
    }

    .today-unit {
      color: var(--vf-text-muted);
      font-size: 0.875rem;
    }

    .today-total {
      margin-inline-start: auto;
      font-size: 1.125rem;
      font-weight: 600;
    }

    .movements {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
    }

    /* A hairline between rows, no full grid (§6). */
    .movement {
      display: flex;
      align-items: baseline;
      gap: var(--vf-space-3);
      padding-block: var(--vf-space-2);
      border-block-end: 1px solid var(--vf-border-subtle, var(--vf-border));
      font-size: 0.875rem;
    }

    .movement:last-child {
      border-block-end: none;
    }

    .movement-product {
      flex: 1;
    }

    .movement-quantity {
      color: var(--vf-text-muted);
    }

    .movement-time {
      color: var(--vf-text-faint);
      font-size: 0.8125rem;
    }
  `,
})
export class DashboardPageComponent {
  protected readonly store = inject(DashboardStore);
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly router = inject(Router);

  /** Fixed-length placeholder list so the skeleton has the band's shape (§9). */
  protected readonly skeletonTiles = [0, 1, 2, 3];

  /** The one primary action (§15.1, DEC-DSH-008) — the day's most frequent, most time-sensitive act. */
  protected goToNewSale(): void {
    void this.router.navigate(['/sales/new']);
  }

  protected todaySales() {
    const section = this.store.dashboard()?.sections.todaySales;
    return section?.status === 'ok' ? section : null;
  }

  protected movements() {
    const section = this.store.dashboard()?.sections.recentMovements;
    return section?.status === 'ok' ? (section.items ?? []) : null;
  }

  /**
   * The sales list, filtered to the clinic's today.
   *
   * **The date comes from the server's response, never from the browser** — device time is a
   * prohibited source for a business date (BR-DSH-003, clinic-date.md), so the client must
   * not compute «today» even for a link.
   */
  protected todayParams(): Record<string, string> {
    const clinicDate = this.store.dashboard()?.clinicDate ?? '';
    return { status: 'committed', dateFrom: clinicDate, dateTo: clinicDate };
  }

  /** Date over time, from the one formatter (§10, STD-FE-042). */
  protected occurredAt(timestamp: string): string {
    const parts = this.format.dateTimeParts(timestamp);
    return parts.time ? `${parts.date} · ${parts.time}` : parts.date;
  }
}
