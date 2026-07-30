import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { ExpiryMonitoringItem } from '../expiry-monitoring.models';

/** The mobile expiry list (expiry monitoring ui.md): a card per batch — product, remaining, expiry. */
@Component({
  selector: 'app-expiry-monitoring-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="cards">
      @for (item of rows(); track item.batchId) {
        <li class="card">
          <div class="card-main">
            <span class="card-product">{{ item.productName }}</span>
            <span class="card-meta vf-num">
              {{ t.t('expiry.column.remaining') }}: {{ format.decimal(item.remainingQuantity) }}
              {{ item.stockUnitName }}
            </span>
          </div>
          <div class="card-side">
            <span class="card-expiry-label">{{ t.t('expiry.column.expiryDate') }}</span>
            <span class="card-expiry vf-num">{{ format.date(item.expiryDate) }}</span>
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

    .card-expiry-label {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .card-expiry {
      font-weight: 600;
    }
  `,
})
export class ExpiryMonitoringCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly ExpiryMonitoringItem[]>();
}
