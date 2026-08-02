import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { ProductListItem } from '../product-list.models';
import { ProductStatusBadgesComponent } from './product-status-badges.component';

/**
 * The mobile experience is a fast lookup tool, not a shrunken table
 * (catalog ui.md §10): a card per product — name, default price, status —
 * three facts readable at arm's length.
 */
@Component({
  selector: 'app-product-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProductStatusBadgesComponent],
  template: `
    <ul class="cards">
      @for (product of rows(); track product.id) {
        <li
          class="card"
          tabindex="0"
          [attr.aria-label]="t.t('products.row.open', { name: product.arabicName })"
          (click)="open.emit(product.id)"
          (keydown.enter)="open.emit(product.id)"
        >
          <div class="card-main">
            <span class="card-name">{{ product.arabicName }}</span>
            <span class="card-secondary">
              {{ product.categoryName }} · {{ product.manufacturerName }}
            </span>
          </div>
          <div class="card-side">
            @if (product.defaultSaleUnitPrice; as price) {
              <span class="card-price vf-num">{{ format.money(price.amount, price.currency) }}</span>
              <span class="card-price-unit">
                {{ t.t('products.price.perUnit', { unit: product.defaultSaleUnitName }) }}
              </span>
            }
            <app-product-status-badges [product]="product" />
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

    .card-main {
      min-inline-size: 0;
    }

    .card-name {
      display: block;
      font-weight: 600;
    }

    .card-secondary {
      display: block;
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
    }

    .card-side {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: var(--vf-space-1);
      text-align: end;
    }

    .card-price {
      font-weight: 600;
    }

    .card-price-unit {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }
  `,
})
export class ProductCardsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly rows = input.required<readonly ProductListItem[]>();
  readonly open = output<string>();
}
