import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { ManufacturerListItem } from '../manufacturer-list.models';
import { ManufacturerStatusBadgeComponent } from './manufacturer-status-badge.component';

/**
 * The mobile manufacturer list (matches catalog ui.md §10): a card per manufacturer —
 * name, state, and the same two actions — readable at arm's length.
 */
@Component({
  selector: 'app-manufacturer-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfButtonComponent, ManufacturerStatusBadgeComponent],
  template: `
    <ul class="cards">
      @for (manufacturer of rows(); track manufacturer.id) {
        <li class="card">
          <div class="card-head">
            <span class="card-name" [class.card-name--muted]="!manufacturer.isActive">{{ manufacturer.name }}</span>
            <app-manufacturer-status-badge [isActive]="manufacturer.isActive" />
          </div>
          <div class="card-actions">
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
export class ManufacturerCardsComponent {
  protected readonly t = inject(TranslationService);

  readonly rows = input.required<readonly ManufacturerListItem[]>();
  readonly rename = output<ManufacturerListItem>();
  readonly toggleActive = output<ManufacturerListItem>();
}
