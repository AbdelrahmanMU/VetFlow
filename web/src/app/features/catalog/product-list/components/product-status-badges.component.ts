import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';
import { ProductListItem } from '../product-list.models';

/**
 * Status badges (catalog ui.md S1): نشط · معطَّل · بلا سعر — text + color +
 * icon, never color alone.
 */
@Component({
  selector: 'app-product-status-badges',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    <span class="badges">
      @if (product().status === 'active') {
        <vf-badge tone="success" icon="pi-check-circle">{{ t.t('products.status.active') }}</vf-badge>
      } @else {
        <vf-badge tone="neutral" icon="pi-ban">{{ t.t('products.status.disabled') }}</vf-badge>
      }
      @if (!product().hasSellingPrice) {
        <vf-badge tone="warning" icon="pi-exclamation-triangle">
          {{ t.t('products.status.noPrice') }}
        </vf-badge>
      }
    </span>
  `,
  styles: `
    .badges {
      display: inline-flex;
      gap: var(--vf-space-1);
      flex-wrap: wrap;
    }
  `,
})
export class ProductStatusBadgesComponent {
  protected readonly t = inject(TranslationService);
  readonly product = input.required<ProductListItem>();
}
