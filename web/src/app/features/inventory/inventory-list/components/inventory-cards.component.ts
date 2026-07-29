import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { InventoryItem } from '../inventory-list.models';

/**
 * The mobile experience is a fast lookup, not a shrunken table (inventory ui.md):
 * a card per product — name, on-hand + stock unit, batch count, and nearest expiry.
 */
@Component({
  selector: 'app-inventory-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="cards">
      @for (item of rows(); track item.productId) {
        <li
          class="card"
          tabindex="0"
          role="button"
          [attr.aria-label]="t.t('inventory.row.open', { product: item.productName })"
          (click)="open.emit(item.productId)"
          (keydown.enter)="open.emit(item.productId)"
        >
          <div class="card-main">
            <span class="card-product">{{ item.productName }}</span>
            <span class="card-meta vf-num">
              {{ t.t('inventory.column.batchCount') }}: {{ format.integer(item.batchCount) }}
              @if (item.nearestExpiry) {
                · {{ t.t('inventory.column.nearestExpiry') }}: {{ format.date(item.nearestExpiry) }}
              }
            </span>
          </div>
          <div class="card-side">
            <span class="card-onhand vf-num">{{ format.decimal(item.onHandQuantity) }}</span>
            <span class="card-unit">{{ item.stockUnitName }}</span>
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
      justify-content: space-between;
      align-items: center;
      gap: var(--vf-space-3);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-3) var(--vf-space-4);
      min-block-size: 4rem;
      cursor: pointer;
    }

    .card:focus-visible {
      outline: none;
      box-shadow: var(--vf-focus-ring);
    }

    .card-main {
      min-inline-size: 0;
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .card-product {
      font-weight: 600;
    }

    .card-meta {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }

    .card-side {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: var(--vf-space-1);
      text-align: end;
    }

    .card-onhand {
      font-weight: 600;
    }

    .card-unit {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }
  `,
})
export class InventoryCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly InventoryItem[]>();
  readonly open = output<string>();
}
