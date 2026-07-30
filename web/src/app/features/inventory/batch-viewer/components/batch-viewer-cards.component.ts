import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { BatchViewerItem } from '../batch-viewer.models';
import { BatchStatusBadgeComponent } from './batch-status-badge.component';

/**
 * The mobile batch list (batch viewer ui.md): a card per batch — remaining/original in the
 * stock unit, expiry, the purchase reference link, and the derived status badge.
 */
@Component({
  selector: 'app-batch-viewer-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BatchStatusBadgeComponent, RouterLink],
  template: `
    <ul class="cards">
      @for (item of rows(); track item.batchId) {
        <li class="card">
          <div class="card-head">
            <a
              class="purchase-link vf-num"
              [routerLink]="['/purchases', item.purchaseInvoiceId]"
              [attr.aria-label]="t.t('batchViewer.row.openPurchase', { reference: item.purchaseReference })"
            >{{ item.purchaseReference }}</a>
            <app-batch-status-badge [status]="item.status" />
          </div>
          <div class="card-body">
            <span class="card-qty vf-num">
              {{ format.decimal(item.remainingQuantity) }} / {{ format.decimal(item.originalQuantity) }}
              {{ item.stockUnitName }}
            </span>
            <span class="card-meta vf-num">
              {{ t.t('batchViewer.column.receiveDate') }}: {{ format.date(item.receiveDate) }}
              · {{ t.t('batchViewer.column.expiryDate') }}:
              {{ item.expiryDate ? format.date(item.expiryDate) : '—' }}
            </span>
            <span class="card-meta vf-num">
              {{ t.t('batchViewer.column.unitCost') }}: {{ format.decimal(item.unitCostSnapshot) }}
              {{ t.t('batchViewer.currency') }}
            </span>
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

    .card-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .purchase-link {
      color: var(--vf-primary);
      font-weight: 600;
      text-decoration: none;
    }

    .card-body {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .card-qty {
      font-weight: 600;
    }

    .card-meta {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }
  `,
})
export class BatchViewerCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly BatchViewerItem[]>();
}
