import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { ProductListItem } from '../product-list.models';

/**
 * Capability flags as quiet icons in one column (catalog ui.md S1): splittable,
 * refrigerated, has expiration. Each explains itself on hover and to screen
 * readers — never an icon alone (design language §12).
 */
@Component({
  selector: 'app-capability-icons',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="capabilities">
      @for (capability of capabilities(); track capability.label) {
        <i
          class="pi {{ capability.icon }} capability"
          [title]="capability.label"
          role="img"
          [attr.aria-label]="capability.label"
        ></i>
      } @empty {
        <span class="none" aria-hidden="true">—</span>
      }
    </span>
  `,
  styles: `
    .capabilities {
      display: inline-flex;
      gap: var(--vf-space-2);
      color: var(--vf-text-faint);
    }

    .capability {
      font-size: 0.875rem;
    }

    .none {
      color: var(--vf-text-faint);
    }
  `,
})
export class CapabilityIconsComponent {
  private readonly t = inject(TranslationService);

  readonly product = input.required<ProductListItem>();

  protected readonly capabilities = computed(() => {
    const product = this.product();
    const list: { icon: string; label: string }[] = [];
    if (product.isSplittable) {
      list.push({ icon: 'pi-clone', label: this.t.t('products.capability.splittable') });
    }

    if (product.isRefrigerated) {
      list.push({ icon: 'pi-asterisk', label: this.t.t('products.capability.refrigerated') });
    }

    if (product.hasExpiration) {
      list.push({ icon: 'pi-calendar', label: this.t.t('products.capability.hasExpiration') });
    }

    return list;
  });
}
