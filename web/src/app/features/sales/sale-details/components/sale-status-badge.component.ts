import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';
import { SaleStatus } from '../sale-details.models';

/**
 * Sales-invoice status badge (sales ui.md, BR-SAL-003): مسودة (neutral) · مُثبَّتة (success) —
 * text + colour + icon, never colour alone. Two states only; «ملغاة» was not introduced
 * (DEC-SAL-009 — open).
 */
@Component({
  selector: 'app-sale-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    @switch (status()) {
      @case ('committed') {
        <vf-badge tone="success" icon="pi-check-circle">{{ t.t('sales.status.committed') }}</vf-badge>
      }
      @default {
        <vf-badge tone="neutral" icon="pi-file-edit">{{ t.t('sales.status.draft') }}</vf-badge>
      }
    }
  `,
})
export class SaleStatusBadgeComponent {
  protected readonly t = inject(TranslationService);
  readonly status = input.required<SaleStatus>();
}
