import { ChangeDetectionStrategy, Component, computed, effect, inject, input, model, output, signal } from '@angular/core';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../../shared/ui-kit/input/vf-date-input.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { PurchaseLine, ReceivePurchaseInvoicePayload } from '../purchase-lines.models';

/**
 * حوار تأكيد استلام الفاتورة (purchasing ui.md, REQ-PUR-005): receiving is an irreversible inventory
 * event, so it always requires an explicit confirmation — the deliberate contrast with the immediate,
 * no-confirm line delete (DEC-PUR-005). For each line whose product requires expiry (BR-PUR-013), a
 * required expiry date is captured in the dialog; the dialog blocks confirmation until every required
 * date is set. The page owns the write (it emits {@link confirmed}). Opening resets the form.
 */
@Component({
  selector: 'app-receive-purchase-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfDialogComponent, VfDateInputComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('purchaseDetails.receive.dialog.title')" [(visible)]="visible">
      @if (serverError()) {
        <p class="banner" role="alert">{{ t.t('purchaseDetails.receive.dialog.error') }}</p>
      }

      <p class="warn">{{ t.t('purchaseDetails.receive.dialog.irreversible') }}</p>

      @if (expiryLines().length > 0) {
        <p class="hint">{{ t.t('purchaseDetails.receive.dialog.expiryHint') }}</p>
        <div class="lines">
          @for (line of expiryLines(); track line.id) {
            <vf-date-input
              [label]="line.productName"
              [required]="true"
              [value]="expiryByLine()[line.id] ?? null"
              (valueChange)="setExpiry(line.id, $event)"
              [error]="errorFor(line.id)"
            />
          }
        </div>
      }

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onConfirm()">
          {{ saving() ? t.t('purchaseDetails.receive.dialog.saving') : t.t('purchaseDetails.receive.dialog.confirm') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('purchaseDetails.receive.dialog.cancel') }}
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
      margin: 0 0 var(--vf-space-3);
      color: var(--vf-text);
      font-weight: 600;
    }

    .hint {
      margin: 0 0 var(--vf-space-2);
      color: var(--vf-text-secondary);
    }

    .lines {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
    }
  `,
})
export class ReceivePurchaseDialogComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly lines = input<readonly PurchaseLine[]>([]);
  readonly saving = input(false);
  readonly serverError = input<string | null>(null);
  readonly confirmed = output<ReceivePurchaseInvoicePayload>();

  protected readonly expiryByLine = signal<Record<string, string | null>>({});
  protected readonly submitted = signal(false);

  protected readonly expiryLines = computed(() => this.lines().filter((line) => line.requiresExpiry));

  constructor() {
    // Reset the captured dates each time the dialog opens (a stale value never lingers).
    effect(() => {
      if (this.visible()) {
        this.expiryByLine.set({});
        this.submitted.set(false);
      }
    });
  }

  protected setExpiry(lineId: string, value: string | null): void {
    this.expiryByLine.update((current) => ({ ...current, [lineId]: value }));
  }

  protected errorFor(lineId: string): string | null {
    return this.submitted() && !this.expiryByLine()[lineId]
      ? this.t.t('purchaseDetails.receive.dialog.expiryRequired')
      : null;
  }

  protected onConfirm(): void {
    this.submitted.set(true);

    const expiries = this.expiryByLine();
    const missing = this.expiryLines().some((line) => !expiries[line.id]);
    if (missing) {
      return;
    }

    this.confirmed.emit({
      lines: this.expiryLines().map((line) => ({ lineId: line.id, expiryDate: expiries[line.id] ?? null })),
    });
  }
}
