import { ChangeDetectionStrategy, Component, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { PossibleDuplicate } from '../product-editor.models';

/**
 * The possible-duplicate comparison dialog (catalog ui.md §5/§9, WF-CAT-001/أ1):
 * shows the similar products beside what the user is entering, with two equal
 * choices — open the existing product, or "this is different, continue saving".
 * A warning, never a block; the decision is always the user's (DEC-CAT-018).
 */
@Component({
  selector: 'app-duplicate-warning-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDialogComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('editor.duplicate.title')" [(visible)]="visible">
      <p class="lead">{{ t.t('editor.duplicate.body') }}</p>

      <div class="mine">
        <span class="mine-label">{{ t.t('editor.duplicate.you') }}</span>
        <span class="mine-name">{{ enteredName() }}</span>
        @if (enteredDetail(); as detail) {
          <span class="mine-detail">{{ detail }}</span>
        }
      </div>

      <ul class="list">
        @for (duplicate of duplicates(); track duplicate.id) {
          <li class="item">
            <div class="item-info">
              <span class="item-name">{{ duplicate.arabicName }}</span>
              <span class="item-meta">{{ metaOf(duplicate) }}</span>
            </div>
            <vf-button variant="secondary" icon="pi-external-link" (pressed)="openExisting.emit(duplicate.id)">
              {{ t.t('editor.duplicate.openExisting') }}
            </vf-button>
          </li>
        }
      </ul>

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" (pressed)="continueSaving.emit()">
          {{ t.t('editor.duplicate.continue') }}
        </vf-button>
      </div>
    </vf-dialog>
  `,
  styles: `
    .lead {
      margin: 0;
      color: var(--vf-text-secondary);
    }

    .mine {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius-small);
      background: var(--vf-primary-soft);
    }

    .mine-label {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
    }

    .mine-name {
      font-weight: 600;
    }

    .mine-detail {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
    }

    .list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
    }

    .item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
      padding: var(--vf-space-3);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius-small);
    }

    .item-info {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      min-inline-size: 0;
    }

    .item-name {
      font-weight: 500;
    }

    .item-meta {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }
  `,
})
export class DuplicateWarningDialogComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly duplicates = input.required<readonly PossibleDuplicate[]>();
  readonly enteredName = input.required<string>();
  readonly enteredSize = input<string | null>(null);
  readonly enteredConcentration = input<string | null>(null);
  readonly openExisting = output<string>();
  readonly continueSaving = output<void>();

  protected enteredDetail(): string | null {
    return joinParts([this.enteredSize(), this.enteredConcentration()]);
  }

  protected metaOf(duplicate: PossibleDuplicate): string {
    const meta = joinParts([duplicate.manufacturerName, duplicate.size, duplicate.concentration]);
    return meta ?? duplicate.manufacturerName;
  }
}

function joinParts(parts: readonly (string | null)[]): string | null {
  const kept = parts.filter((part): part is string => !!part);
  return kept.length > 0 ? kept.join(' · ') : null;
}
