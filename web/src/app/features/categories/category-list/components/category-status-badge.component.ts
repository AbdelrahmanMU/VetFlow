import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';

/**
 * Category state badge (categories ui.md): نشط · غير نشط — text + color + icon,
 * never color alone (design language §6, §11), matching the product status badge.
 */
@Component({
  selector: 'app-category-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    @if (isActive()) {
      <vf-badge tone="success" icon="pi-check-circle">{{ t.t('categories.status.active') }}</vf-badge>
    } @else {
      <vf-badge tone="neutral" icon="pi-ban">{{ t.t('categories.status.inactive') }}</vf-badge>
    }
  `,
})
export class CategoryStatusBadgeComponent {
  protected readonly t = inject(TranslationService);
  readonly isActive = input.required<boolean>();
}
