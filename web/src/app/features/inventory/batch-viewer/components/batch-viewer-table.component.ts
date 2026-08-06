import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MessageKey } from '../../../../core/i18n/ar';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { BatchViewerItem, BatchViewerSort, BatchViewerSortField } from '../batch-viewer.models';
import { BatchStatusBadgeComponent } from './batch-status-badge.component';

interface BatchColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly sortable: boolean;
  readonly numeric: boolean;
}

/** The nine frozen columns (BR-INV-020); only the whitelisted three are sortable (BR-INV-027). */
const BATCH_COLUMNS: readonly BatchColumn[] = [
  { id: 'batchId', labelKey: 'batchViewer.column.batchId', sortable: false, numeric: false },
  { id: 'purchaseReference', labelKey: 'batchViewer.column.purchaseReference', sortable: false, numeric: false },
  { id: 'receiveDate', labelKey: 'batchViewer.column.receiveDate', sortable: true, numeric: false },
  { id: 'originalQuantity', labelKey: 'batchViewer.column.originalQuantity', sortable: false, numeric: true },
  { id: 'remainingQuantity', labelKey: 'batchViewer.column.remainingQuantity', sortable: true, numeric: true },
  { id: 'stockUnit', labelKey: 'batchViewer.column.stockUnit', sortable: false, numeric: false },
  { id: 'unitCost', labelKey: 'batchViewer.column.unitCost', sortable: false, numeric: true },
  { id: 'expiryDate', labelKey: 'batchViewer.column.expiryDate', sortable: true, numeric: false },
  { id: 'status', labelKey: 'batchViewer.column.status', sortable: false, numeric: false },
];

/**
 * The desktop/tablet batch table (batch viewer ui.md): every batch of the product with
 * its nine frozen fields. The purchase reference is a navigation link to the owning
 * invoice (BR-INV-024, DEC-INV-010); read-only, no action column (BR-INV-018).
 */
@Component({
  selector: 'app-batch-viewer-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, BatchStatusBadgeComponent, RouterLink],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      [sort]="sort()"
      stateKey="vetflow.inventory.batchViewer.table.v1"
      [tableLabel]="t.t('batchViewer.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-item let-cols="columns">
        <tr>
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('batchId') {
                <td>
                  <span class="batch-id vf-num" [attr.title]="item.batchId">{{ shortId(item.batchId) }}</span>
                </td>
              }
              @case ('purchaseReference') {
                <td>
                  <a
                    class="purchase-link vf-num"
                    [routerLink]="['/purchases', item.purchaseInvoiceId]"
                    [attr.aria-label]="t.t('batchViewer.row.openPurchase', { reference: item.purchaseReference })"
                  >{{ item.purchaseReference }}</a>
                </td>
              }
              @case ('receiveDate') {
                <!--
                  An instant, not a business date (the contract sends a DateTimeOffset),
                  so it needs dateOfInstant: date() refuses a timestamp, and until
                  2026-08-06 that refusal echoed the raw ISO string into this column.
                -->
                <td class="cell-secondary vf-num">{{ format.dateOfInstant(item.receiveDate) }}</td>
              }
              @case ('originalQuantity') {
                <td class="vf-td--numeric vf-num">{{ format.decimal(item.originalQuantity) }}</td>
              }
              @case ('remainingQuantity') {
                <td class="vf-td--numeric vf-num">
                  <span class="remaining">{{ format.decimal(item.remainingQuantity) }}</span>
                </td>
              }
              @case ('stockUnit') {
                <td class="cell-secondary">{{ item.stockUnitName }}</td>
              }
              @case ('unitCost') {
                <td class="vf-td--numeric cell-secondary vf-num">
                  {{ format.moneyAmount(item.unitCostSnapshot) }} {{ t.t('batchViewer.currency') }}
                </td>
              }
              @case ('expiryDate') {
                <td class="cell-secondary vf-num">
                  {{ item.expiryDate ? format.date(item.expiryDate) : '—' }}
                </td>
              }
              @case ('status') {
                <td><app-batch-status-badge [status]="item.status" /></td>
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

    .batch-id {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .purchase-link {
      color: var(--vf-primary);
      font-weight: 600;
      text-decoration: none;
    }

    .purchase-link:hover,
    .purchase-link:focus-visible {
      text-decoration: underline;
    }

    .remaining {
      font-weight: 600;
    }

    .cell-secondary {
      color: var(--vf-text-secondary);
    }
  `,
})
export class BatchViewerTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly BatchViewerItem[]>();
  readonly sort = input.required<BatchViewerSort>();
  readonly sortChange = output<BatchViewerSort>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    BATCH_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: column.sortable,
      numeric: column.numeric,
    })),
  );

  /** Shows the first segment of the stable identifier for visual distinction (BR-INV-025); the full id is the title. */
  protected shortId(batchId: string): string {
    return batchId.split('-')[0];
  }

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as BatchViewerSortField, direction: sort.direction });
  }
}
