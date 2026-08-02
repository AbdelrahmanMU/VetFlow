import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MessageKey } from '../../../../core/i18n/ar';
import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MovementHistoryItem, MovementSource } from '../movement-history.models';
import { MovementTypeBadgeComponent } from './movement-type-badge.component';

const SOURCE_LABELS: Readonly<Record<MovementSource, MessageKey>> = {
  purchasing: 'history.source.purchasing',
  sales: 'history.source.sales',
  inventory: 'history.source.inventory',
};

/**
 * The mobile history list (inventory ui.md): a card per movement carrying the same seven fields
 * as the table (BR-INV-041) — nothing is dropped on the small screen, only re-laid out.
 */
@Component({
  selector: 'app-movement-history-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MovementTypeBadgeComponent, RouterLink],
  template: `
    <ul class="cards">
      @for (item of rows(); track item.movementId) {
        <li class="card">
          <div class="card-head">
            <app-movement-type-badge [type]="item.type" />
            <!-- The same two-line stamp as the table, so one habit reads both. -->
            <span class="card-date vf-num">
              <span class="stamp-date">{{ format.dateTimeParts(item.occurredAt).date }}</span>
              <span class="stamp-time">{{ format.dateTimeParts(item.occurredAt).time }}</span>
            </span>
          </div>

          <span class="card-product">{{ item.productName }}</span>

          <div class="card-figures">
            <span
              class="card-quantity vf-num"
              [class.quantity--in]="item.quantity > 0"
              [class.quantity--out]="item.quantity < 0"
            >
              {{ signed(item.quantity) }} <span class="unit">{{ item.stockUnitName }}</span>
            </span>
            <span class="card-batch vf-num" [attr.title]="item.batchId">
              {{ t.t('history.column.batch') }}: {{ shortId(item.batchId) }}
            </span>
          </div>

          <div class="card-foot">
            <span class="card-source">{{ sourceLabel(item.source) }}</span>
            @switch (item.referenceTarget) {
              @case ('purchaseInvoice') {
                <a
                  class="reference-link vf-num"
                  [routerLink]="['/purchases', item.referenceId]"
                  [attr.aria-label]="t.t('history.row.openPurchase', { reference: item.referenceLabel ?? '' })"
                >{{ item.referenceLabel }}</a>
              }
              @case ('salesInvoice') {
                <a
                  class="reference-link vf-num"
                  [routerLink]="['/sales', item.referenceId]"
                  [attr.aria-label]="t.t('history.row.openSale', { reference: item.referenceLabel ?? '' })"
                >{{ item.referenceLabel }}</a>
              }
              @default {
                <span class="card-source">—</span>
              }
            }
          </div>
        </li>
      }
    </ul>
  `,
  styles: `
    .cards {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
    }

    .card {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-3) var(--vf-space-4);
    }

    .card-head,
    .card-figures,
    .card-foot {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
    }

    .card-product {
      font-weight: 600;
      min-inline-size: 0;
      overflow-wrap: anywhere;
    }

    .card-date,
    .card-batch,
    .card-source {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .card-date {
      text-align: end;
    }

    .stamp-date,
    .stamp-time {
      display: block;
    }

    .stamp-time {
      color: var(--vf-text-faint);
    }

    .card-quantity {
      font-weight: 600;
    }

    .quantity--in {
      color: var(--vf-success);
    }

    .quantity--out {
      color: var(--vf-danger);
    }

    .unit {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
      font-weight: 400;
    }

    .reference-link {
      color: var(--vf-primary);
      text-decoration: none;
      font-weight: 600;
    }
  `,
})
export class MovementHistoryCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly MovementHistoryItem[]>();

  protected shortId(batchId: string): string {
    return batchId.split('-')[0];
  }

  protected sourceLabel(source: MovementSource): string {
    return this.t.t(SOURCE_LABELS[source]);
  }

  protected signed(quantity: number): string {
    const formatted = this.format.decimal(quantity);
    return quantity > 0 ? `+${formatted}` : formatted;
  }
}
