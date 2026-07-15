import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { LookupOption, ProductFilters, ProductStatus } from '../product-list.models';

/**
 * The filters side panel (catalog ui.md S1): the ten documented filters minus
 * the two stock-owned ones (deferred with Inventory/Monitoring — owner ruling).
 * Filters apply immediately; the applied set is always visible as chips.
 */
@Component({
  selector: 'app-product-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('products.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('products.filter.category')"
          [placeholder]="t.t('products.filter.any')"
          [filterable]="true"
          [optionList]="categorySelectOptions()"
          [value]="filters().category"
          (valueChange)="patch({ category: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.manufacturer')"
          [placeholder]="t.t('products.filter.any')"
          [filterable]="true"
          [optionList]="manufacturerSelectOptions()"
          [value]="filters().manufacturer"
          (valueChange)="patch({ manufacturer: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.nature')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="natureSelectOptions()"
          [value]="filters().nature"
          (valueChange)="patch({ nature: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.status')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="statusOptions()"
          [value]="filters().status"
          (valueChange)="patch({ status: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.refrigerated')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="booleanOptions()"
          [value]="filters().refrigerated"
          (valueChange)="patch({ refrigerated: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.hasExpiration')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="booleanOptions()"
          [value]="filters().hasExpiration"
          (valueChange)="patch({ hasExpiration: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.splittable')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="booleanOptions()"
          [value]="filters().splittable"
          (valueChange)="patch({ splittable: $event })"
        />
        <vf-select
          [label]="t.t('products.filter.hasSellingPrice')"
          [placeholder]="t.t('products.filter.any')"
          [optionList]="booleanOptions()"
          [value]="filters().hasSellingPrice"
          (valueChange)="patch({ hasSellingPrice: $event })"
        />
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('products.filters.clearAll') }}
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
export class ProductFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<ProductFilters>();
  readonly categoryOptions = input.required<readonly LookupOption[]>();
  readonly manufacturerOptions = input.required<readonly LookupOption[]>();
  readonly natureOptions = input.required<readonly LookupOption[]>();
  readonly filtersChange = output<ProductFilters>();
  readonly cleared = output<void>();

  protected readonly categorySelectOptions = computed(() => toSelectOptions(this.categoryOptions()));
  protected readonly manufacturerSelectOptions = computed(() =>
    toSelectOptions(this.manufacturerOptions()),
  );
  protected readonly natureSelectOptions = computed(() => toSelectOptions(this.natureOptions()));

  protected readonly statusOptions = computed<readonly VfSelectOption<ProductStatus>[]>(() => [
    { label: this.t.t('products.filter.active'), value: 'active' },
    { label: this.t.t('products.filter.disabled'), value: 'disabled' },
  ]);

  protected readonly booleanOptions = computed<readonly VfSelectOption<boolean>[]>(() => [
    { label: this.t.t('products.filter.yes'), value: true },
    { label: this.t.t('products.filter.no'), value: false },
  ]);

  protected patch(change: Partial<ProductFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}

function toSelectOptions(options: readonly LookupOption[]): readonly VfSelectOption<LookupOption>[] {
  return options.map((option) => ({ label: option.name, value: option }));
}
