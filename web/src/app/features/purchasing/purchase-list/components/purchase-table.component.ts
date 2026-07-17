import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MessageKey } from '../../../../core/i18n/ar';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { PurchaseListItem, PurchaseSort, PurchaseSortField } from '../purchase-list.models';
import { PurchaseStatusBadgeComponent } from './purchase-status-badge.component';

interface PurchaseColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly sortable: boolean;
  readonly numeric: boolean;
}

/** The six frozen columns (BR-PUR-004); only the whitelisted five are sortable. */
const PURCHASE_COLUMNS: readonly PurchaseColumn[] = [
  { id: 'number', labelKey: 'purchases.column.number', sortable: true, numeric: false },
  { id: 'supplier', labelKey: 'purchases.column.supplier', sortable: true, numeric: false },
  { id: 'invoiceDate', labelKey: 'purchases.column.invoiceDate', sortable: true, numeric: false },
  { id: 'status', labelKey: 'purchases.column.status', sortable: true, numeric: false },
  { id: 'total', labelKey: 'purchases.column.total', sortable: true, numeric: true },
  { id: 'createdAt', labelKey: 'purchases.column.createdAt', sortable: false, numeric: false },
];

/**
 * The desktop/tablet purchase table (purchasing ui.md): the system number leads,
 * the supplier name follows, the total aligns as a fixed-width number, and the
 * status shows as a badge.
 */
@Component({
  selector: 'app-purchase-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, PurchaseStatusBadgeComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      [sort]="sort()"
      stateKey="vetflow.purchasing.invoices.table.v1"
      [tableLabel]="t.t('purchases.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-invoice let-cols="columns">
        <tr
          tabindex="0"
          class="clickable-row"
          [attr.aria-label]="t.t('purchases.row.open', { number: invoice.number })"
          (click)="open.emit(invoice.id)"
          (keydown.enter)="open.emit(invoice.id)"
        >
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('number') {
                <td><span class="number vf-num">{{ invoice.number }}</span></td>
              }
              @case ('supplier') {
                <td>
                  <span class="supplier">{{ invoice.supplierName }}</span>
                  @if (invoice.supplierInvoiceReference) {
                    <span class="supplier-secondary">
                      {{ t.t('purchases.reference.label') }}: <bdi>{{ invoice.supplierInvoiceReference }}</bdi>
                    </span>
                  }
                </td>
              }
              @case ('invoiceDate') {
                <td class="cell-secondary vf-num">{{ format.date(invoice.invoiceDate) }}</td>
              }
              @case ('status') {
                <td><app-purchase-status-badge [status]="invoice.status" /></td>
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

    .supplier {
      display: block;
      font-weight: 600;
      color: var(--vf-text);
    }

    .supplier-secondary {
      display: block;
      font-size: var(--vf-text-caption);
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
export class PurchaseTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly PurchaseListItem[]>();
  readonly sort = input.required<PurchaseSort>();
  readonly sortChange = output<PurchaseSort>();
  readonly open = output<string>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    PURCHASE_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: column.sortable,
      numeric: column.numeric,
    })),
  );

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as PurchaseSortField, direction: sort.direction });
  }
}
