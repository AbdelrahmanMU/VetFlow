import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../../shared/ui-kit/input/vf-date-input.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { PurchaseFilters, PurchaseStatus } from '../purchase-list.models';

/**
 * The filters side panel (purchasing ui.md, BR-PUR-004): status and the
 * invoice-date range — nothing else. Filters apply immediately; the applied set
 * is always visible as chips on the page.
 */
@Component({
  selector: 'app-purchase-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfDateInputComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('purchases.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('purchases.filter.status')"
          [placeholder]="t.t('purchases.filter.any')"
          [optionList]="statusOptions()"
          [value]="filters().status"
          (valueChange)="patch({ status: $event })"
        />
        <vf-date-input
          [label]="t.t('purchases.filter.dateFrom')"
          [value]="filters().dateFrom"
          [max]="filters().dateTo"
          (valueChange)="patch({ dateFrom: $event })"
        />
        <vf-date-input
          [label]="t.t('purchases.filter.dateTo')"
          [value]="filters().dateTo"
          [min]="filters().dateFrom"
          (valueChange)="patch({ dateTo: $event })"
        />
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('purchases.filters.clearAll') }}
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
export class PurchaseFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<PurchaseFilters>();
  readonly filtersChange = output<PurchaseFilters>();
  readonly cleared = output<void>();

  protected readonly statusOptions = computed<readonly VfSelectOption<PurchaseStatus>[]>(() => [
    { label: this.t.t('purchases.status.draft'), value: 'draft' },
    { label: this.t.t('purchases.status.received'), value: 'received' },
    { label: this.t.t('purchases.status.cancelled'), value: 'cancelled' },
  ]);

  protected patch(change: Partial<PurchaseFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}
