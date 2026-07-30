import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfSkeletonComponent } from '../../../../shared/ui-kit/skeleton/vf-skeleton.component';

/** Loading state shaped like the batch table — never a spinner (batch viewer ui.md, STD-FE-031). */
@Component({
  selector: 'app-batch-viewer-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfSkeletonComponent],
  template: `
    <div class="skeleton-table" role="status" [attr.aria-label]="t.t('batchViewer.loading')">
      <div class="skeleton-row skeleton-row--header">
        @for (width of headerWidths; track $index) {
          <vf-skeleton [width]="width" height="0.75rem" />
        }
      </div>
      @for (row of rows; track row) {
        <div class="skeleton-row">
          @for (width of cellWidths; track $index) {
            <vf-skeleton [width]="width" height="0.875rem" />
          }
        </div>
      }
    </div>
  `,
  styles: `
    .skeleton-table {
      display: flex;
      flex-direction: column;
      background: var(--vf-surface);
    }

    .skeleton-row {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr 1fr 1fr 0.8fr 1fr 1fr 1fr;
      align-items: center;
      gap: var(--vf-space-3);
      padding: var(--vf-space-3);
      border-block-end: 1px solid var(--vf-border);
    }

    .skeleton-row--header {
      background: var(--vf-bg);
      padding-block: var(--vf-space-2);
    }

    @media (max-width: 768px) {
      .skeleton-row {
        grid-template-columns: 1fr 1fr;
      }

      .skeleton-row > :nth-child(n + 3) {
        display: none;
      }
    }
  `,
})
export class BatchViewerSkeletonComponent {
  protected readonly t = inject(TranslationService);
  protected readonly rows = Array.from({ length: 6 }, (_, index) => index);
  protected readonly headerWidths = ['55%', '60%', '50%', '45%', '45%', '40%', '50%', '50%', '55%'];
  protected readonly cellWidths = ['60%', '65%', '55%', '40%', '40%', '35%', '45%', '55%', '50%'];
}
