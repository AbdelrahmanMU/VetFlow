import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';
import { BatchStatus } from '../batch-viewer.models';

/**
 * Batch status badge (batch viewer ui.md): نشطة · مستنفدة — text + color + icon, never
 * color alone (design language §6, §11). Only two derived statuses exist (BR-INV-021).
 */
@Component({
  selector: 'app-batch-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    @if (status() === 'active') {
      <vf-badge tone="success" icon="pi-check-circle">{{ t.t('batchViewer.status.active') }}</vf-badge>
    } @else {
      <vf-badge tone="neutral" icon="pi-inbox">{{ t.t('batchViewer.status.depleted') }}</vf-badge>
    }
  `,
})
export class BatchStatusBadgeComponent {
  protected readonly t = inject(TranslationService);
  readonly status = input.required<BatchStatus>();
}
