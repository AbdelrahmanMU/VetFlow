import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { CategoryListItem } from '../category-list.models';
import { CategoryStatusBadgeComponent } from './category-status-badge.component';

/**
 * The mobile category list (categories ui.md; matches catalog ui.md §10): a card
 * per category — name, state, and the same two actions — readable at arm's length.
 */
@Component({
  selector: 'app-category-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfButtonComponent, CategoryStatusBadgeComponent],
  template: `
    <ul class="cards">
      @for (category of rows(); track category.id) {
        <li class="card">
          <div class="card-head">
            <span class="card-name" [class.card-name--muted]="!category.isActive">{{ category.name }}</span>
            <app-category-status-badge [isActive]="category.isActive" />
          </div>
          <div class="card-actions">
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
        </li>
      }
    </ul>
  `,
  styles: `
    .cards {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
    }

    .card {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-3) var(--vf-space-4);
    }

    .card-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
    }

    .card-name {
      font-weight: 600;
      min-inline-size: 0;
    }

    .card-name--muted {
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .card-actions {
      display: flex;
      gap: var(--vf-space-2);
    }
  `,
})
export class CategoryCardsComponent {
  protected readonly t = inject(TranslationService);

  readonly rows = input.required<readonly CategoryListItem[]>();
  readonly rename = output<CategoryListItem>();
  readonly toggleActive = output<CategoryListItem>();
}
