import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { MessageKey } from '../../../../core/i18n/ar';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { InventoryItem, InventorySort, InventorySortField } from '../inventory-list.models';

interface InventoryColumn {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly sortable: boolean;
  readonly numeric: boolean;
}

/** The five frozen columns (BR-INV-006..010); only the whitelisted three are sortable (BR-INV-014). */
const INVENTORY_COLUMNS: readonly InventoryColumn[] = [
  { id: 'product', labelKey: 'inventory.column.product', sortable: true, numeric: false },
  { id: 'onHand', labelKey: 'inventory.column.onHand', sortable: true, numeric: true },
  { id: 'stockUnit', labelKey: 'inventory.column.stockUnit', sortable: false, numeric: false },
  { id: 'batchCount', labelKey: 'inventory.column.batchCount', sortable: false, numeric: true },
  { id: 'nearestExpiry', labelKey: 'inventory.column.nearestExpiry', sortable: true, numeric: false },
];

/**
 * The desktop/tablet inventory table (inventory ui.md): the product leads, the
 * on-hand quantity aligns as a fixed-width number next to its stock unit, then the
 * active-batch count and the nearest expiry ("—" when none). Rows navigate to the
 * future Batch Viewer (BR-INV-015) — read-only, no action column (BR-INV-006).
 */
@Component({
  selector: 'app-inventory-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      [sort]="sort()"
      stateKey="vetflow.inventory.projection.table.v1"
      [tableLabel]="t.t('inventory.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-item let-cols="columns">
        <tr
          tabindex="0"
          class="clickable-row"
          [attr.aria-label]="t.t('inventory.row.open', { product: item.productName })"
          (click)="open.emit(item.productId)"
          (keydown.enter)="open.emit(item.productId)"
        >
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('product') {
                <td><span class="product">{{ item.productName }}</span></td>
              }
              @case ('onHand') {
                <td class="vf-td--numeric">
                  <span class="on-hand vf-num">{{ format.decimal(item.onHandQuantity) }}</span>
                </td>
              }
              @case ('stockUnit') {
                <td class="cell-secondary">{{ item.stockUnitName }}</td>
              }
              @case ('batchCount') {
                <td class="vf-td--numeric cell-secondary vf-num">{{ format.integer(item.batchCount) }}</td>
              }
              @case ('nearestExpiry') {
                <td class="cell-secondary vf-num">
                  {{ item.nearestExpiry ? format.date(item.nearestExpiry) : '—' }}
                </td>
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

    .product {
      font-weight: 600;
      color: var(--vf-text);
    }

    .on-hand {
      font-weight: 600;
    }

    .cell-secondary {
      color: var(--vf-text-secondary);
    }
  `,
})
export class InventoryTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly InventoryItem[]>();
  readonly sort = input.required<InventorySort>();
  readonly sortChange = output<InventorySort>();
  readonly open = output<string>();

  protected readonly tableColumns = computed<readonly VfTableColumn[]>(() =>
    INVENTORY_COLUMNS.map((column) => ({
      id: column.id,
      label: this.t.t(column.labelKey),
      sortable: column.sortable,
      numeric: column.numeric,
    })),
  );

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as InventorySortField, direction: sort.direction });
  }
}
