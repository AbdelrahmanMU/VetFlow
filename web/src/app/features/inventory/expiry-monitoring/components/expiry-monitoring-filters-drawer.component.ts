import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { CategoryOption, ExpiryMonitoringFilters } from '../expiry-monitoring.models';

/**
 * The expiry filters side panel (expiry monitoring ui.md, BR-INV-035): category, "expired"
 * and "expiring soon" (30-day horizon) — nothing else. Both are derived filters, not statuses.
 */
@Component({
  selector: 'app-expiry-monitoring-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfCheckboxComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('expiry.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('expiry.filter.category')"
          [placeholder]="t.t('expiry.filter.any')"
          [filterable]="true"
          [optionList]="categorySelectOptions()"
          [value]="filters().category"
          (valueChange)="patch({ category: $event })"
        />
        <vf-checkbox [checked]="filters().expired" (toggled)="patch({ expired: !filters().expired })">
          {{ t.t('expiry.filter.expired') }}
        </vf-checkbox>
        <vf-checkbox
          [checked]="filters().expiringSoon"
          (toggled)="patch({ expiringSoon: !filters().expiringSoon })"
        >
          {{ t.t('expiry.filter.expiringSoon') }}
        </vf-checkbox>
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('expiry.filters.clearAll') }}
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
export class ExpiryMonitoringFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<ExpiryMonitoringFilters>();
  readonly categoryOptions = input.required<readonly CategoryOption[]>();
  readonly filtersChange = output<ExpiryMonitoringFilters>();
  readonly cleared = output<void>();

  protected readonly categorySelectOptions = computed<readonly VfSelectOption<CategoryOption>[]>(() =>
    this.categoryOptions().map((option) => ({ label: option.name, value: option })),
  );

  protected patch(change: Partial<ExpiryMonitoringFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}
