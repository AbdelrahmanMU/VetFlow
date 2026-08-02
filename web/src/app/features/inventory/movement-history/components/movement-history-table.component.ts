import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MessageKey } from '../../../../core/i18n/ar';
import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfTableColumn, VfTableComponent } from '../../../../shared/ui-kit/table/vf-table.component';
import { MovementHistoryItem, MovementSource } from '../movement-history.models';
import { MovementTypeBadgeComponent } from './movement-type-badge.component';

interface HistoryColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly numeric: boolean;
}

/**
 * The seven frozen fields, in the documented order (BR-INV-041). No sort control — the order is
 * fixed newest-first (BR-INV-044) — and no action column: the history is immutable (BR-INV-039).
 */
const HISTORY_COLUMNS: readonly HistoryColumn[] = [
  { id: 'date', labelKey: 'history.column.date', numeric: false },
  { id: 'type', labelKey: 'history.column.type', numeric: false },
  { id: 'product', labelKey: 'history.column.product', numeric: false },
  { id: 'batch', labelKey: 'history.column.batch', numeric: false },
  { id: 'quantity', labelKey: 'history.column.quantity', numeric: true },
  { id: 'reference', labelKey: 'history.column.reference', numeric: false },
  { id: 'source', labelKey: 'history.column.source', numeric: false },
];

const SOURCE_LABELS: Readonly<Record<MovementSource, MessageKey>> = {
  purchasing: 'history.source.purchasing',
  sales: 'history.source.sales',
  inventory: 'history.source.inventory',
};

/**
 * The desktop/tablet history table (inventory ui.md, REQ-INV-005). Read-only, immutable, and
 * showing exactly what the ledger recorded — including the sign of the quantity, which is the
 * direction of the movement (BR-INV-064) and is never stripped to an absolute value.
 */
@Component({
  selector: 'app-movement-history-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, MovementTypeBadgeComponent, RouterLink],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      stateKey="vetflow.inventory.history.table.v1"
      [tableLabel]="t.t('history.table.label')"
    >
      <ng-template #row let-item let-cols="columns">
        <tr>
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('date') {
                <!-- Date over time (owner ruling, 2026-08-02): the column is scanned
                     for the day first, and the time only settles ties within it. -->
                <td class="cell-secondary vf-num">
                  <span class="stamp-date">{{ format.dateTimeParts(item.occurredAt).date }}</span>
                  <span class="stamp-time">{{ format.dateTimeParts(item.occurredAt).time }}</span>
                </td>
              }
              @case ('type') {
                <td><app-movement-type-badge [type]="item.type" /></td>
              }
              @case ('product') {
                <td><span class="product">{{ item.productName }}</span></td>
              }
              @case ('batch') {
                <td>
                  <span class="batch-id vf-num" [attr.title]="item.batchId">{{ shortId(item.batchId) }}</span>
                </td>
              }
              @case ('quantity') {
                <td class="vf-td--numeric vf-num">
                  <span [class.quantity--in]="item.quantity > 0" [class.quantity--out]="item.quantity < 0">
                    {{ signed(item.quantity) }}
                  </span>
                  <span class="unit">{{ item.stockUnitName }}</span>
                </td>
              }
              @case ('reference') {
                <td>
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
                      <span class="cell-secondary">—</span>
                    }
                  }
                </td>
              }
              @case ('source') {
                <td class="cell-secondary">{{ sourceLabel(item.source) }}</td>
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

    .stamp-date {
      display: block;
    }

    .stamp-time {
      display: block;
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }

    .product {
      font-weight: 600;
      color: var(--vf-text);
    }

    .batch-id,
    .cell-secondary {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .quantity--in {
      font-weight: 600;
      color: var(--vf-success);
    }

    .quantity--out {
      font-weight: 600;
      color: var(--vf-danger);
    }

    .unit {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
      margin-inline-start: var(--vf-space-1);
    }

    .reference-link {
      color: var(--vf-primary);
      text-decoration: none;
      font-weight: 600;
    }

    .reference-link:hover,
    .reference-link:focus-visible {
      text-decoration: underline;
    }
  `,
})
export class MovementHistoryTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly MovementHistoryItem[]>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    HISTORY_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: false,
      numeric: column.numeric,
    })),
  );

  protected shortId(batchId: string): string {
    return batchId.split('-')[0];
  }

  protected sourceLabel(source: MovementSource): string {
    return this.t.t(SOURCE_LABELS[source]);
  }

  /**
   * Keeps the direction visible: an increase is prefixed with "+", a decrease already carries its
   * own "-" from the formatter. The value itself is never altered (BR-INV-058: no rounding).
   */
  protected signed(quantity: number): string {
    const formatted = this.format.decimal(quantity);
    return quantity > 0 ? `+${formatted}` : formatted;
  }
}
