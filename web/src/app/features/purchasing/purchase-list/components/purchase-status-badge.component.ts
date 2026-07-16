import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';
import { PurchaseStatus } from '../purchase-list.models';

/**
 * Purchase-invoice status badge (purchasing ui.md, AC-PUR-002): مسودة (neutral) ·
 * مستلمة (success) · ملغاة (danger) — text + color + icon, never color alone.
 */
@Component({
  selector: 'app-purchase-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    @switch (status()) {
      @case ('received') {
        <vf-badge tone="success" icon="pi-check-circle">{{ t.t('purchases.status.received') }}</vf-badge>
      }
      @case ('cancelled') {
        <vf-badge tone="danger" icon="pi-times-circle">{{ t.t('purchases.status.cancelled') }}</vf-badge>
      }
      @default {
        <vf-badge tone="neutral" icon="pi-file-edit">{{ t.t('purchases.status.draft') }}</vf-badge>
      }
    }
  `,
})
export class PurchaseStatusBadgeComponent {
  protected readonly t = inject(TranslationService);
  readonly status = input.required<PurchaseStatus>();
}
