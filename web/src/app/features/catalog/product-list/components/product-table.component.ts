import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import {
  VfSortState,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { ProductColumnPreferences } from '../product-columns';
import { ProductListItem, ProductSort, ProductSortField } from '../product-list.models';
import { CapabilityIconsComponent } from './capability-icons.component';
import { ProductStatusBadgesComponent } from './product-status-badges.component';

/**
 * The desktop/tablet product table (catalog ui.md S1): the Arabic name leads
 * with size and concentration as a secondary line; prices align as fixed-width
 * numbers with the default sale unit stated.
 */
@Component({
  selector: 'app-product-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, CapabilityIconsComponent, ProductStatusBadgesComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="tableColumns()"
      [sort]="sort()"
      stateKey="vetflow.catalog.products.table.v1"
      [tableLabel]="t.t('products.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-product let-cols="columns">
        <tr
          tabindex="0"
          class="clickable-row"
          [attr.aria-label]="t.t('products.row.open', { name: product.arabicName })"
          (click)="open.emit(product.id)"
          (keydown.enter)="open.emit(product.id)"
        >
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('name') {
                <td>
                  <span class="name">{{ product.arabicName }}</span>
                  @if (secondaryParts(product).length > 0) {
                    <span class="name-secondary">
                      <!-- bdi isolates each mixed-direction part so Arabic and Latin never interleave -->
                      @for (part of secondaryParts(product); track part; let last = $last) {
                        <bdi>{{ part }}</bdi>
                        @if (!last) {
                          <span aria-hidden="true"> · </span>
                        }
                      }
                    </span>
                  }
                </td>
              }
              @case ('category') {
                <td class="cell-secondary">{{ product.categoryName }}</td>
              }
              @case ('manufacturer') {
                <td class="cell-secondary">{{ product.manufacturerName }}</td>
              }
              @case ('nature') {
                <td class="cell-secondary">{{ product.natureName }}</td>
              }
              @case ('storageUnit') {
                <td class="cell-secondary">{{ product.storageUnitName }}</td>
              }
              @case ('price') {
                <td class="vf-td--numeric">
                  @if (product.defaultSaleUnitPrice; as price) {
                    <span class="price vf-num">{{ format.money(price.amount, price.currency) }}</span>
                    <span class="price-unit">
                      {{ t.t('products.price.perUnit', { unit: product.defaultSaleUnitName }) }}
                    </span>
                  } @else {
                    <span class="cell-faint" aria-hidden="true">—</span>
                  }
                </td>
              }
              @case ('capabilities') {
                <td><app-capability-icons [product]="product" /></td>
              }
              @case ('status') {
                <td><app-product-status-badges [product]="product" /></td>
              }
              @case ('actions') {
                <td class="action-col">
                  <!-- The row itself opens details; this makes that discoverable
                       rather than leaving the row looking like static text
                       (catalog ui.md §3 «إجراءات مباشرة على الصف»). -->
                  <button type="button" class="row-action" (click)="onOpenAction($event, product.id)">
                    {{ t.t('products.details.open') }}
                  </button>
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

    .action-col {
      text-align: end;
      white-space: nowrap;
    }

    .row-action {
      padding: var(--vf-space-1) var(--vf-space-2);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius-small);
      background: var(--vf-surface);
      color: var(--vf-text-secondary);
      font-family: inherit;
      font-size: var(--vf-text-caption);
      cursor: pointer;
      /* §3: row actions surface on hover and do not clutter the resting row. */
      opacity: 0;
      transition: opacity 120ms ease;
    }

    .clickable-row:hover .row-action,
    .clickable-row:focus-within .row-action,
    .row-action:focus-visible {
      opacity: 1;
    }

    /* Touch has no hover, so the affordance must simply be there (§5 اللمس أولًا). */
    @media (hover: none) {
      .row-action {
        opacity: 1;
      }
    }

    .row-action:hover {
      color: var(--vf-text);
      border-color: var(--vf-border-strong);
    }

    .name {
      display: block;
      font-weight: 600;
      color: var(--vf-text);
    }

    .name-secondary {
      display: block;
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
    }

    .cell-secondary {
      color: var(--vf-text-secondary);
    }

    .cell-faint {
      color: var(--vf-text-faint);
    }

    .price {
      display: block;
      font-weight: 600;
    }

    .price-unit {
      display: block;
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }
  `,
})
export class ProductTableComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly columnPreferences = inject(ProductColumnPreferences);

  readonly rows = input.required<readonly ProductListItem[]>();
  readonly sort = input.required<ProductSort>();
  readonly sortChange = output<ProductSort>();
  readonly open = output<string>();

  /** The row is clickable too, so the action must not open details twice. */
  protected onOpenAction(event: Event, productId: string): void {
    event.stopPropagation();
    this.open.emit(productId);
  }

  protected readonly tableColumns = computed(() =>
    this.columnPreferences.toTableColumns((key) => this.t.t(key)),
  );

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as ProductSortField, direction: sort.direction });
  }

  protected secondaryParts(product: ProductListItem): string[] {
    return [product.size, product.concentration, product.englishName].filter(
      (part): part is string => Boolean(part),
    );
  }
}
