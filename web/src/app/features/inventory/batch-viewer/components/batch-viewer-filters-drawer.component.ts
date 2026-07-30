import { ChangeDetectionStrategy, Component, computed, inject, model, output } from '@angular/core';
import { input } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { BatchStatus, BatchViewerFilters } from '../batch-viewer.models';

/**
 * The batch filters side panel (batch viewer ui.md, BR-INV-026): batch status
 * (Active/Depleted), "expired" and "expiring soon" (30-day horizon) — nothing else.
 * "Expired" is a filter, never a status (DEC-INV-012).
 */
@Component({
  selector: 'app-batch-viewer-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfCheckboxComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('batchViewer.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('batchViewer.filter.status')"
          [placeholder]="t.t('batchViewer.filter.any')"
          [optionList]="statusOptions()"
          [value]="filters().status"
          (valueChange)="patch({ status: $event })"
        />
        <vf-checkbox [checked]="filters().expired" (toggled)="patch({ expired: !filters().expired })">
          {{ t.t('batchViewer.filter.expired') }}
        </vf-checkbox>
        <vf-checkbox
          [checked]="filters().expiringSoon"
          (toggled)="patch({ expiringSoon: !filters().expiringSoon })"
        >
          {{ t.t('batchViewer.filter.expiringSoon') }}
        </vf-checkbox>
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('batchViewer.filters.clearAll') }}
          </vf-button>
        </div>
      </div>
    </vf-drawer>
  `,
  styles: `
    .filters {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
    }

    .filters-footer {
      margin-block-start: var(--vf-space-2);
      display: flex;
      justify-content: flex-start;
    }
  `,
})
export class BatchViewerFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<BatchViewerFilters>();
  readonly filtersChange = output<BatchViewerFilters>();
  readonly cleared = output<void>();

  protected readonly statusOptions = computed<readonly VfSelectOption<BatchStatus>[]>(() => [
    { label: this.t.t('batchViewer.status.active'), value: 'active' },
    { label: this.t.t('batchViewer.status.depleted'), value: 'depleted' },
  ]);

  protected patch(change: Partial<BatchViewerFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}
