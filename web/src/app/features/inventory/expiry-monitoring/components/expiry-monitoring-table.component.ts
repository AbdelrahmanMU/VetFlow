import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MessageKey } from '../../../../core/i18n/ar';
import { VfTableColumn, VfTableComponent } from '../../../../shared/ui-kit/table/vf-table.component';
import { ExpiryMonitoringItem } from '../expiry-monitoring.models';

interface ExpiryColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly numeric: boolean;
}

/** The four frozen fields (BR-INV-034). No user-selectable sort — the order is fixed (BR-INV-037). */
const EXPIRY_COLUMNS: readonly ExpiryColumn[] = [
  { id: 'product', labelKey: 'expiry.column.product', numeric: false },
  { id: 'batch', labelKey: 'expiry.column.batch', numeric: false },
  { id: 'remaining', labelKey: 'expiry.column.remaining', numeric: true },
  { id: 'expiryDate', labelKey: 'expiry.column.expiryDate', numeric: false },
];

/**
 * The desktop/tablet expiry table (expiry monitoring ui.md): the product, the batch
 * identifier, the remaining quantity in the stock unit, and the expiry date — the earliest
 * expiry first (BR-INV-037). Read-only, no action column, no alerts (BR-INV-032).
 */
@Component({
  selector: 'app-expiry-monitoring-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      stateKey="vetflow.inventory.expiry.table.v1"
      [tableLabel]="t.t('expiry.table.label')"
    >
      <ng-template #row let-item let-cols="columns">
        <tr>
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('product') {
                <td><span class="product">{{ item.productName }}</span></td>
              }
              @case ('batch') {
                <td>
                  <span class="batch-id vf-num" [attr.title]="item.batchId">{{ shortId(item.batchId) }}</span>
                </td>
              }
              @case ('remaining') {
                <td class="vf-td--numeric vf-num">
                  {{ format.decimal(item.remainingQuantity) }}
                  <span class="unit">{{ item.stockUnitName }}</span>
                </td>
              }
              @case ('expiryDate') {
                <td class="expiry vf-num">{{ format.date(item.expiryDate) }}</td>
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

    .product {
      font-weight: 600;
      color: var(--vf-text);
    }

    .batch-id {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .unit {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
    }

    .expiry {
      font-weight: 600;
    }
  `,
})
export class ExpiryMonitoringTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly ExpiryMonitoringItem[]>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    EXPIRY_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: false,
      numeric: column.numeric,
    })),
  );

  protected shortId(batchId: string): string {
    return batchId.split('-')[0];
  }
}
