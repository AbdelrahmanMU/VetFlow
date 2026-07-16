import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { ManufacturerListItem, ManufacturerSort, ManufacturerSortField } from '../manufacturer-list.models';
import { ManufacturerStatusBadgeComponent } from './manufacturer-status-badge.component';

/**
 * The desktop/tablet manufacturer table: the Arabic name, its state badge, and the
 * row actions — rename and activate/deactivate — reusing the one table language
 * (design language §6) exactly as the product and category lists do.
 */
@Component({
  selector: 'app-manufacturer-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, VfButtonComponent, ManufacturerStatusBadgeComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="columns()"
      [sort]="sortState()"
      stateKey="vetflow.manufacturers.table.v1"
      [tableLabel]="t.t('manufacturers.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-manufacturer let-cols="columns">
        <tr tabindex="0">
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('name') {
                <td>
                  <span class="name" [class.name--muted]="!manufacturer.isActive">{{ manufacturer.name }}</span>
                </td>
              }
              @case ('status') {
                <td><app-manufacturer-status-badge [isActive]="manufacturer.isActive" /></td>
              }
              @case ('actions') {
                <td>
                  <div class="actions">
                    <vf-button variant="quiet" icon="pi-pencil" (pressed)="rename.emit(manufacturer)">
                      {{ t.t('manufacturers.action.rename') }}
                    </vf-button>
                    @if (manufacturer.isActive) {
                      <vf-button variant="quiet" icon="pi-ban" (pressed)="toggleActive.emit(manufacturer)">
                        {{ t.t('manufacturers.action.deactivate') }}
                      </vf-button>
                    } @else {
                      <vf-button variant="quiet" icon="pi-check-circle" (pressed)="toggleActive.emit(manufacturer)">
                        {{ t.t('manufacturers.action.activate') }}
                      </vf-button>
                    }
                  </div>
                </td>
              }
            }
          }
        </tr>
      </ng-template>
    </vf-table>
  `,
  styles: `
    :host {
      display: block;
      min-block-size: 0;
    }

    .name {
      font-weight: 600;
      color: var(--vf-text);
    }

    .name--muted {
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .actions {
      display: flex;
      gap: var(--vf-space-1);
      flex-wrap: wrap;
    }
  `,
})
export class ManufacturerTableComponent {
  protected readonly t = inject(TranslationService);

  readonly rows = input.required<readonly ManufacturerListItem[]>();
  readonly sort = input.required<ManufacturerSort>();
  readonly sortChange = output<ManufacturerSort>();
  readonly rename = output<ManufacturerListItem>();
  readonly toggleActive = output<ManufacturerListItem>();

  protected readonly sortState = computed<VfSortState>(() => this.sort());

  protected readonly columns = computed<readonly VfTableColumn[]>(() => [
    { id: 'name', label: this.t.t('manufacturers.column.name'), sortable: true },
    { id: 'status', label: this.t.t('manufacturers.column.status'), sortable: true },
    { id: 'actions', label: this.t.t('manufacturers.column.actions'), sortable: false },
  ]);

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as ManufacturerSortField, direction: sort.direction });
  }
}
