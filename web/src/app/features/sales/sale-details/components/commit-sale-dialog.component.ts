import { ChangeDetectionStrategy, Component, computed, inject, input, model, output } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { CommitRejection } from '../sale-lines.models';

/**
 * حوار تأكيد إثبات البيع (sales ui.md, REQ-SAL-003): committing is irreversible and consumes
 * inventory, so it is **always** preceded by an explicit confirmation that says so — the same
 * treatment as the approved receive-purchase confirmation, and the deliberate contrast with the
 * immediate, no-confirm line delete (DEC-PUR-005).
 *
 * A rejected commit changes nothing — the invoice stays a draft with all its lines (BR-SAL-012) —
 * so the dialog stays open and shows why, branching on the classified reason only, never on
 * message text (STD-FE-037):
 * - **تعارض تزامن** (BR-INV-056, DEC-INV-023) → the message states the stock changed and the
 *   primary action becomes **إعادة المحاولة**, which re-attempts against the new state.
 * - **نقص المخزون** (AC-SAL-009) → names the products the server named. It never says the balance
 *   is zero: expired stock can read positive in the projection yet be unsaleable (DEC-INV-021).
 * - **تحويل غير تامّ** (BR-INV-058, AC-SAL-013) → names the line; nothing is rounded.
 *
 * The page owns the write (it emits {@link confirmed}).
 */
@Component({
  selector: 'app-commit-sale-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDialogComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('saleDetails.commit.dialog.title')" [(visible)]="visible">
      @if (rejectionMessage(); as message) {
        <p class="banner" role="alert">{{ message }}</p>
      }

      <p class="warn">{{ t.t('saleDetails.commit.dialog.irreversible') }}</p>

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check-circle" [disabled]="saving()" (pressed)="confirmed.emit()">
          {{ confirmLabel() }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('saleDetails.commit.dialog.cancel') }}
        </vf-button>
      </div>
    </vf-dialog>
  `,
  styles: `
    .banner {
      margin: 0 0 var(--vf-space-3);
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius-small);
      background: var(--vf-danger-soft, #fbeae8);
      color: var(--vf-danger, #b42318);
      font-weight: 500;
    }

    .warn {
      margin: 0;
      color: var(--vf-text);
      font-weight: 600;
    }
  `,
})
export class CommitSaleDialogComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly saving = input(false);
  /** The classified reason of the last refusal, or null when nothing has been refused yet. */
  readonly rejection = input<CommitRejection | null>(null);
  readonly confirmed = output<void>();

  protected readonly rejectionMessage = computed(() => {
    const rejection = this.rejection();
    if (!rejection) {
      return null;
    }

    const products = rejection.products;
    switch (rejection.failure) {
      case 'insufficientStock':
        return products
          ? this.t.t('saleDetails.commit.error.insufficientStock', { products })
          : this.t.t('saleDetails.commit.error.insufficientStockUnnamed');
      case 'concurrencyConflict':
        return this.t.t('saleDetails.commit.error.concurrencyConflict');
      case 'inexactConversion':
        return products
          ? this.t.t('saleDetails.commit.error.inexactConversion', { products })
          : this.t.t('saleDetails.commit.error.inexactConversionUnnamed');
      default:
        return this.t.t('saleDetails.commit.error.other');
    }
  });

  /** Only a concurrency conflict is worth retrying as-is: the allocation runs again on the new state. */
  protected readonly confirmLabel = computed(() => {
    if (this.saving()) {
      return this.t.t('saleDetails.commit.dialog.saving');
    }

    return this.rejection()?.failure === 'concurrencyConflict'
      ? this.t.t('saleDetails.commit.dialog.retry')
      : this.t.t('saleDetails.commit.dialog.confirm');
  });
}
