import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import {
  VfSortState,
  VfTableColumn,
  VfTableComponent,
} from '../../../../shared/ui-kit/table/vf-table.component';
import { CategoryListItem, CategorySort, CategorySortField } from '../category-list.models';
import { CategoryStatusBadgeComponent } from './category-status-badge.component';

/**
 * The desktop/tablet category table (categories ui.md): the Arabic name, its
 * state badge, and the row actions — rename and activate/deactivate — reusing the
 * one table language (design language §6) exactly as the product list does.
 */
@Component({
  selector: 'app-category-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfTableComponent, VfButtonComponent, CategoryStatusBadgeComponent],
  template: `
    <vf-table
      [rows]="rows()"
      [columns]="columns()"
      [sort]="sortState()"
      stateKey="vetflow.categories.table.v1"
      [tableLabel]="t.t('categories.table.label')"
      (sortChange)="onSortChange($event)"
    >
      <ng-template #row let-category let-cols="columns">
        <tr tabindex="0">
          @for (col of cols; track col.id) {
            @switch (col.id) {
              @case ('name') {
                <td>
                  <span class="name" [class.name--muted]="!category.isActive">{{ category.name }}</span>
                </td>
              }
              @case ('status') {
                <td><app-category-status-badge [isActive]="category.isActive" /></td>
              }
              @case ('actions') {
                <td>
                  <div class="actions">
                    <vf-button variant="quiet" icon="pi-pencil" (pressed)="rename.emit(category)">
                      {{ t.t('categories.action.rename') }}
                    </vf-button>
                    @if (category.isActive) {
                      <vf-button variant="quiet" icon="pi-ban" (pressed)="toggleActive.emit(category)">
                        {{ t.t('categories.action.deactivate') }}
                      </vf-button>
                    } @else {
                      <vf-button variant="quiet" icon="pi-check-circle" (pressed)="toggleActive.emit(category)">
                        {{ t.t('categories.action.activate') }}
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
export class CategoryTableComponent {
  protected readonly t = inject(TranslationService);

  readonly rows = input.required<readonly CategoryListItem[]>();
  readonly sort = input.required<CategorySort>();
  readonly sortChange = output<CategorySort>();
  readonly rename = output<CategoryListItem>();
  readonly toggleActive = output<CategoryListItem>();

  protected readonly sortState = computed<VfSortState>(() => this.sort());

  protected readonly columns = computed<readonly VfTableColumn[]>(() => [
    { id: 'name', label: this.t.t('categories.column.name'), sortable: true },
    { id: 'status', label: this.t.t('categories.column.status'), sortable: true },
    { id: 'actions', label: this.t.t('categories.column.actions'), sortable: false },
  ]);

  protected onSortChange(sort: VfSortState): void {
    this.sortChange.emit({ field: sort.field as CategorySortField, direction: sort.direction });
  }
}
