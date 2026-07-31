import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MessageKey } from '../../../../core/i18n/ar';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { SaleStatusBadgeComponent } from '../../sale-details/components/sale-status-badge.component';
import { SalesListItem, SalesListSort, SalesListSortField } from '../sales-list.models';

interface SalesColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly sortable: boolean;
  readonly numeric: boolean;
}

/** The six frozen columns (BR-SAL-019); only the whitelisted five are sortable. */
const SALES_COLUMNS: readonly SalesColumn[] = [
  { id: 'number', labelKey: 'salesList.column.number', sortable: true, numeric: false },
  { id: 'customer', labelKey: 'salesList.column.customer', sortable: true, numeric: false },
  { id: 'saleDate', labelKey: 'salesList.column.saleDate', sortable: true, numeric: false },
  { id: 'status', labelKey: 'salesList.column.status', sortable: true, numeric: false },
  { id: 'total', labelKey: 'salesList.column.total', sortable: true, numeric: true },
  { id: 'createdAt', labelKey: 'salesList.column.createdAt', sortable: false, numeric: false },
];

/**
 * The desktop/tablet sales table (sales ui.md): the system number leads, the
 * customer name follows (a dash when the invoice has none — DEC-SAL-002), the
 * total aligns as a fixed-width number, and the status shows as a badge.
 */
@Component({
  selector: 'app-sales-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, SaleStatusBadgeComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      [sort]="sort()"
      stateKey="vetflow.sales.invoices.table.v1"
      [tableLabel]="t.t('salesList.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-invoice let-cols="columns">
        <tr
          tabindex="0"
          class="clickable-row"
          [attr.aria-label]="t.t('salesList.row.open', { number: invoice.number })"
          (click)="open.emit(invoice.id)"
          (keydown.enter)="open.emit(invoice.id)"
        >
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('number') {
                <td><span class="number vf-num">{{ invoice.number }}</span></td>
              }
              @case ('customer') {
                <td>
                  @if (invoice.customerName) {
                    <span class="customer">{{ invoice.customerName }}</span>
                  } @else {
                    <span class="customer-none" aria-hidden="true">—</span>
                  }
                </td>
              }
              @case ('saleDate') {
                <td class="cell-secondary vf-num">{{ format.date(invoice.saleDate) }}</td>
              }
              @case ('status') {
                <td><app-sale-status-badge [status]="invoice.status" /></td>
              }
              @case ('total') {
                <td class="vf-td--numeric">
                  <span class="total vf-num">{{ format.money(invoice.total.amount, invoice.total.currency) }}</span>
                </td>
              }
              @case ('createdAt') {
                <td class="cell-faint vf-num">{{ format.date(invoice.createdAt.slice(0, 10)) }}</td>
              }
            }
          }
        </tr>
      </ng-template>
    </vf-table>
  `,
  styles: `
    :host {
      display: block;
      min-block-size: 0;
    }

    .clickable-row {
      cursor: pointer;
    }

    .number {
      font-weight: 600;
      color: var(--vf-text);
    }

    .customer {
      display: block;
      font-weight: 600;
      color: var(--vf-text);
    }

    .customer-none {
      color: var(--vf-text-faint);
    }

    .cell-secondary {
      color: var(--vf-text-secondary);
    }

    .cell-faint {
      color: var(--vf-text-faint);
    }

    .total {
      font-weight: 600;
    }
  `,
})
export class SalesTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly SalesListItem[]>();
  readonly sort = input.required<SalesListSort>();
  readonly sortChange = output<SalesListSort>();
  readonly open = output<string>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    SALES_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: column.sortable,
      numeric: column.numeric,
    })),
  );

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as SalesListSortField, direction: sort.direction });
  }
}
