import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';

/**
 * Manufacturer state badge (matches the category status badge): نشط · غير نشط —
 * text + color + icon, never color alone (design language §6, §11).
 */
@Component({
  selector: 'app-manufacturer-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    @if (isActive()) {
      <vf-badge tone="success" icon="pi-check-circle">{{ t.t('manufacturers.status.active') }}</vf-badge>
    } @else {
      <vf-badge tone="neutral" icon="pi-ban">{{ t.t('manufacturers.status.inactive') }}</vf-badge>
    }
  `,
})
export class ManufacturerStatusBadgeComponent {
  protected readonly t = inject(TranslationService);
  readonly isActive = input.required<boolean>();
}
