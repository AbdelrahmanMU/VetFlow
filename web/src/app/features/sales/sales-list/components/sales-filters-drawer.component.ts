import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../../shared/ui-kit/input/vf-date-input.component';
import { VfDrawerComponent } from '../../../../shared/ui-kit/drawer/vf-drawer.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { SaleListStatus, SalesListFilters } from '../sales-list.models';

/**
 * The filters side panel (BR-SAL-019): status — the two sales states only, no
 * «ملغاة» (BR-SAL-003) — and the sale-date range. Nothing else. Filters apply
 * immediately; the applied set is always visible as chips on the page.
 */
@Component({
  selector: 'app-sales-filters-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDrawerComponent, VfSelectComponent, VfDateInputComponent, VfButtonComponent],
  template: `
    <vf-drawer [header]="t.t('salesList.filters.title')" [(visible)]="visible">
      <div class="filters">
        <vf-select
          [label]="t.t('salesList.filter.status')"
          [placeholder]="t.t('salesList.filter.any')"
          [optionList]="statusOptions()"
          [value]="filters().status"
          (valueChange)="patch({ status: $event })"
        />
        <vf-date-input
          [label]="t.t('salesList.filter.dateFrom')"
          [value]="filters().dateFrom"
          [max]="filters().dateTo"
          (valueChange)="patch({ dateFrom: $event })"
        />
        <vf-date-input
          [label]="t.t('salesList.filter.dateTo')"
          [value]="filters().dateTo"
          [min]="filters().dateFrom"
          (valueChange)="patch({ dateTo: $event })"
        />
        <div class="filters-footer">
          <vf-button variant="quiet" icon="pi-filter-slash" (pressed)="cleared.emit()">
            {{ t.t('salesList.filters.clearAll') }}
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
export class SalesFiltersDrawerComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly filters = input.required<SalesListFilters>();
  readonly filtersChange = output<SalesListFilters>();
  readonly cleared = output<void>();

  protected readonly statusOptions = computed<readonly VfSelectOption<SaleListStatus>[]>(() => [
    { label: this.t.t('sales.status.draft'), value: 'draft' },
    { label: this.t.t('sales.status.committed'), value: 'committed' },
  ]);

  protected patch(change: Partial<SalesListFilters>): void {
    this.filtersChange.emit({ ...this.filters(), ...change });
  }
}
