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
import { FormControl, FormRecord, ReactiveFormsModule } from '@angular/forms';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { SubmitGuidanceDirective } from '../../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../../core/validation/validation-focus.service';
import { RuleMessageOverrides } from '../../../../core/validation/validation-messages';
import { vfValidators } from '../../../../core/validation/validators';
import { VfBannerComponent } from '../../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../../shared/ui-kit/input/vf-date-input.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfFormFieldComponent } from '../../../../shared/ui-kit/form-field/vf-form-field.component';
import { PurchaseLine, ReceivePurchaseInvoicePayload } from '../purchase-lines.models';

/**
 * حوار تأكيد استلام الفاتورة (purchasing ui.md, REQ-PUR-005): receiving is an irreversible inventory
 * event, so it always requires an explicit confirmation — the deliberate contrast with the immediate,
 * no-confirm line delete (DEC-PUR-005). For each line whose product requires expiry (BR-PUR-013), a
 * required expiry date is captured in the dialog; the dialog blocks confirmation until every required
 * date is set. The page owns the write (it emits {@link confirmed}). Opening resets the form.
 *
 * Validation-foundation adoption (validation-and-guidance.md, Adoption Epic):
 * an irreversible operation gets full rejection fidelity (STD-UX-037) — the
 * page's failure arrives **classified** and renders per-code in the shared
 * banner, focused on appearance (STD-UX-071), with the confirm action
 * relabelling to retry on a retryable conflict (STD-UX-033). Each per-line
 * expiry runs the three moments through its own `vf-form-field` (blur via the
 * date CVA), and a rejected confirm focuses the **first offending line**
 * (STD-UX-084) through the shared guidance — not one sentence for N lines.
 */
@Component({
  selector: 'app-receive-purchase-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfDialogComponent,
    VfFormFieldComponent,
    VfDateInputComponent,
    VfButtonComponent,
  ],
  template: `
    <vf-dialog [header]="t.t('purchaseDetails.receive.dialog.title')" [(visible)]="visible">
      <form [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="emitConfirm()">
        @if (failureMessage(); as message) {
          <vf-banner tone="error" #failureBanner>{{ message }}</vf-banner>
        }

        <p class="warn">{{ t.t('purchaseDetails.receive.dialog.irreversible') }}</p>

        @if (expiryLines().length > 0) {
          <p class="hint">{{ t.t('purchaseDetails.receive.dialog.expiryHint') }}</p>
          <div class="lines">
            @for (line of expiryLines(); track line.id) {
              @if (form.controls[line.id]; as control) {
                <vf-form-field [label]="line.productName" [required]="true" [messages]="expiryMessages">
                  <vf-date-input [formControl]="control" />
                </vf-form-field>
              }
            }
          </div>
        }
      </form>

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onConfirm()">
          {{ confirmLabel() }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('purchaseDetails.receive.dialog.cancel') }}
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
  private readonly focus = inject(ValidationFocusService);

  readonly visible = model(false);
  readonly lines = input<readonly PurchaseLine[]>([]);
  readonly saving = input(false);
  /** The page's classified receive failure (ApiErrorMapper output) — null while clean. */
  readonly serverFailure = input<ClassifiedFailure | null>(null);
  readonly confirmed = output<ReceivePurchaseInvoicePayload>();

  /** One required expiry control per line that needs one (BR-PUR-013). */
  protected readonly form = new FormRecord<FormControl<string | null>>({});

  protected readonly expiryMessages: RuleMessageOverrides = {
    required: 'purchaseDetails.receive.dialog.expiryRequired',
  };

  protected readonly expiryLines = computed(() => this.lines().filter((line) => line.requiresExpiry));

  private readonly guide = viewChild(SubmitGuidanceDirective);
  private readonly failureBanner = viewChild('failureBanner', { read: ElementRef });

  protected readonly failureMessage = computed(() => {
    const failure = this.serverFailure();
    return failure ? this.t.t(failure.messageKey, failure.params) : null;
  });

  // A retryable conflict re-offers the same action as a retry (STD-UX-033).
  protected readonly confirmLabel = computed(() => {
    if (this.saving()) {
      return this.t.t('purchaseDetails.receive.dialog.saving');
    }

    return this.serverFailure()?.retryable
      ? this.t.t('errors.retry')
      : this.t.t('purchaseDetails.receive.dialog.confirm');
  });

  constructor() {
    // Rebuild the per-line controls each time the dialog opens (a stale date
    // never lingers) — a fresh moment cycle (STD-UX-014).
    effect(() => {
      if (!this.visible()) {
        return;
      }

      for (const key of Object.keys(this.form.controls)) {
        this.form.removeControl(key);
      }

      for (const line of this.expiryLines()) {
        this.form.addControl(line.id, new FormControl<string | null>(null, vfValidators.required));
      }

      this.guide()?.resetSubmitted();
    });

    // The classified rejection receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.serverFailure()) {
        return;
      }

      const banner = this.failureBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  /** The footer button lives outside the form element, so it triggers the shared
   *  guidance — a rejected confirm focuses the first offending line (STD-UX-084). */
  protected onConfirm(): void {
    this.guide()?.trigger();
  }

  protected emitConfirm(): void {
    const expiries = this.form.getRawValue();
    this.confirmed.emit({
      lines: this.expiryLines().map((line) => ({ lineId: line.id, expiryDate: expiries[line.id] ?? null })),
    });
  }
}
