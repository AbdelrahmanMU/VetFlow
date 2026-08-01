import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { ValidationFocusService } from '../../../../core/validation/validation-focus.service';
import { VfBannerComponent } from '../../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';

/**
 * حوار تأكيد إثبات البيع (sales ui.md, REQ-SAL-003): committing is irreversible and consumes
 * inventory, so it is **always** preceded by an explicit confirmation that says so — the same
 * treatment as the approved receive-purchase confirmation, and the deliberate contrast with the
 * immediate, no-confirm line delete (DEC-PUR-005).
 *
 * A rejected commit changes nothing — the invoice stays a draft with all its lines (BR-SAL-012) —
 * so the dialog stays open and shows why, branching on the classified code only, never on message
 * text (STD-FE-037, through the shared ApiErrorMapper — STD-UX-123):
 * - **تعارض تزامن** (BR-INV-056, DEC-INV-023) → the message states the stock changed and the
 *   primary action becomes **إعادة المحاولة**, which re-attempts against the new state.
 * - **نقص المخزون** (AC-SAL-009) → names the products the server named. It never says the balance
 *   is zero: expired stock can read positive in the projection yet be unsaleable (DEC-INV-021).
 * - **تحويل غير تامّ** (BR-INV-058, AC-SAL-013) → names the line; nothing is rounded.
 *
 * The rejection renders in the shared banner (STD-UX-121) and receives focus on appearance
 * (STD-UX-071). The page owns the write (it emits {@link confirmed}).
 */
@Component({
  selector: 'app-commit-sale-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBannerComponent, VfDialogComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('saleDetails.commit.dialog.title')" [(visible)]="visible">
      @if (rejectionMessage(); as message) {
        <vf-banner tone="error" #rejectionBanner>{{ message }}</vf-banner>
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
    /* Instance spacing only — the banner's own chrome is the shared component's (STD-UX-121). */
    vf-banner {
      margin-block-end: var(--vf-space-3);
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
  private readonly focus = inject(ValidationFocusService);

  readonly visible = model(false);
  readonly saving = input(false);
  /** The classified refusal (ApiErrorMapper output), or null when nothing has been refused yet. */
  readonly rejection = input<ClassifiedFailure | null>(null);
  readonly confirmed = output<void>();

  private readonly rejectionBanner = viewChild('rejectionBanner', { read: ElementRef });

  protected readonly rejectionMessage = computed(() => {
    const failure = this.rejection();
    if (!failure) {
      return null;
    }

    // Insufficient stock names every product that fell short (`products`); an
    // inexact conversion names the single offending line's product
    // (`product`). Both are data the server produced — the UI only renders
    // what it was given (AC-SAL-009/013), with a metadata-less fallback
    // wording for each (STD-UX-034).
    const products = failure.params?.['products'] ?? failure.params?.['product'] ?? null;
    switch (failure.code) {
      case 'VTF-INV-052':
        return products
          ? this.t.t('saleDetails.commit.error.insufficientStock', { products })
          : this.t.t('saleDetails.commit.error.insufficientStockUnnamed');
      case 'VTF-SAL-012':
        return products
          ? this.t.t('saleDetails.commit.error.inexactConversion', { products })
          : this.t.t('saleDetails.commit.error.inexactConversionUnnamed');
      case 'VTF-INV-056':
        return this.t.t('saleDetails.commit.error.concurrencyConflict');
      default:
        // Any other code renders its registry wording; the mapper's `system`
        // override already routed the un-coded case to the ruled fallback.
        return this.t.t(failure.messageKey, failure.params);
    }
  });

  /** Only a concurrency conflict is worth retrying as-is: the allocation runs again on the new state (STD-UX-033). */
  protected readonly confirmLabel = computed(() => {
    if (this.saving()) {
      return this.t.t('saleDetails.commit.dialog.saving');
    }

    return this.rejection()?.retryable
      ? this.t.t('saleDetails.commit.dialog.retry')
      : this.t.t('saleDetails.commit.dialog.confirm');
  });

  constructor() {
    // The rejection receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.rejection()) {
        return;
      }

      const banner = this.rejectionBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }
}
