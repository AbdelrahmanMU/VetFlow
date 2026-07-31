import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { SaleStatusBadgeComponent } from '../../sale-details/components/sale-status-badge.component';
import { SalesListItem } from '../sales-list.models';

/**
 * The mobile experience is a fast lookup, not a shrunken table (sales ui.md): a
 * card per invoice — number, customer (dash when absent), total, date, status.
 */
@Component({
  selector: 'app-sales-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SaleStatusBadgeComponent],
  template: `
    <ul class="cards">
      @for (invoice of rows(); track invoice.id) {
        <li
          class="card"
          tabindex="0"
          role="button"
          [attr.aria-label]="t.t('salesList.row.open', { number: invoice.number })"
          (click)="open.emit(invoice.id)"
          (keydown.enter)="open.emit(invoice.id)"
        >
          <div class="card-main">
            <span class="card-number vf-num">{{ invoice.number }}</span>
            <span class="card-customer">{{ invoice.customerName ?? '—' }}</span>
            <span class="card-date vf-num">{{ format.date(invoice.saleDate) }}</span>
          </div>
          <div class="card-side">
            <span class="card-total vf-num">{{ format.money(invoice.total.amount, invoice.total.currency) }}</span>
            <app-sale-status-badge [status]="invoice.status" />
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

    .card-number {
      font-weight: 600;
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
    }

    .card-customer {
      font-weight: 600;
    }

    .card-date {
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

    .card-total {
      font-weight: 600;
    }
  `,
})
export class SalesCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly SalesListItem[]>();
  readonly open = output<string>();
}
