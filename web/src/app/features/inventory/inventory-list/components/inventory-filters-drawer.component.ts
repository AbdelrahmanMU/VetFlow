import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { CategoryOption, InventoryFilters } from '../inventory-list.models';

/**
 * The filters side panel (inventory ui.md, BR-INV-014): category, "expiring soon"
 * (30-day horizon) and "out of stock" — nothing else. "Low stock" is deferred
 * (DEC-INV-004) and absent. Filters apply immediately; the applied set is always
 * visible as chips on the page.
 */
@Component({
  selector: 'app-inventory-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfCheckboxComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('inventory.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('inventory.filter.category')"
          [placeholder]="t.t('inventory.filter.any')"
          [filterable]="true"
          [optionList]="categorySelectOptions()"
          [value]="filters().category"
          (valueChange)="patch({ category: $event })"
        />
        <vf-checkbox [checked]="filters().expiringSoon" (toggled)="patch({ expiringSoon: !filters().expiringSoon })">
          {{ t.t('inventory.filter.expiringSoon') }}
        </vf-checkbox>
        <vf-checkbox [checked]="filters().outOfStock" (toggled)="patch({ outOfStock: !filters().outOfStock })">
          {{ t.t('inventory.filter.outOfStock') }}
        </vf-checkbox>
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('inventory.filters.clearAll') }}
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
export class InventoryFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<InventoryFilters>();
  readonly categoryOptions = input.required<readonly CategoryOption[]>();
  readonly filtersChange = output<InventoryFilters>();
  readonly cleared = output<void>();

  protected readonly categorySelectOptions = computed<readonly VfSelectOption<CategoryOption>[]>(() =>
    this.categoryOptions().map((option) => ({ label: option.name, value: option })),
  );

  protected patch(change: Partial<InventoryFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}
