import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfSkeletonComponent } from '../../../../shared/ui-kit/skeleton/vf-skeleton.component';

/**
 * Loading state shaped like the table itself — never a spinner
 * (inventory ui.md states, STD-FE-031).
 */
@Component({
  selector: 'app-inventory-list-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfSkeletonComponent],
  template: `
    <div class="skeleton-table" role="status" [attr.aria-label]="t.t('inventory.loading')">
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
      grid-template-columns: 2fr 1fr 1fr 1fr 1.2fr;
      align-items: center;
      gap: var(--vf-space-4);
      padding: var(--vf-space-3) var(--vf-space-3);
      border-block-end: 1px solid var(--vf-border);
    }

    .skeleton-row--header {
      background: var(--vf-bg);
      padding-block: var(--vf-space-2);
    }

    @media (max-width: 768px) {
      .skeleton-row {
        grid-template-columns: 2fr 1fr;
      }

      .skeleton-row > :nth-child(n + 3) {
        display: none;
      }
    }
  `,
})
export class InventoryListSkeletonComponent {
  protected readonly t = inject(TranslationService);
  protected readonly rows = Array.from({ length: 8 }, (_, index) => index);
  protected readonly headerWidths = ['60%', '50%', '45%', '40%', '55%'];
  protected readonly cellWidths = ['70%', '55%', '40%', '35%', '55%'];
}
