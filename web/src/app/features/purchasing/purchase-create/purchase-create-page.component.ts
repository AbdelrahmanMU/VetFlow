import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MessageKey } from '../../../core/i18n/ar';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ApiErrorMapper, ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { projectServerFieldErrors } from '../../../core/validation/server-errors';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';
import { VfBannerComponent } from '../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfFormFieldComponent } from '../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfDateInputComponent } from '../../../shared/ui-kit/input/vf-date-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { PurchaseCreateApiService } from './purchase-create-api.service';
import { PurchaseCreateForm, buildPurchaseCreateForm } from './purchase-create.forms';
import { CreatePurchasePayload } from './purchase-create.models';

/**
 * شاشة إنشاء فاتورة الشراء (purchasing ui.md, REQ-PUR-003): create a purchase
 * invoice **header** only — supplier name + invoice date required, supplier
 * reference + notes optional. The status is always Draft and the number is
 * generated server-side (BR-PUR-002/003) — no controls; there are no line items
 * or total in this slice (total is 0 server-side, DEC-PUR-002).
 *
 * Validation-foundation adoption (validation-and-guidance.md, the owner-ruled
 * Adoption Test): fields render through `vf-form-field` with the shared
 * default wordings — ui.md rules field-by-field errors (AC-PUR-007) with no
 * contextual copy, so no overrides (STD-UX-111). Submit runs through the
 * shared guidance (moment 3, first-invalid focus — STD-UX-012/070); dates now
 * blur-validate through the date input's CVA (moment 2). Failures classify
 * through the ApiErrorMapper (STD-UX-123): server field errors project back
 * onto their fields (STD-UX-019), anything else renders as a focusable banner
 * (STD-UX-071) that clears on the next relevant edit (STD-UX-035).
 */
@Component({
  selector: 'app-purchase-create-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchaseCreateApiService],
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfFormFieldComponent,
    VfButtonComponent,
    VfTextInputComponent,
    VfDateInputComponent,
    VfTextareaComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('purchaseCreate.title') }}</h1>
      </header>

      <form class="form" [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="save()">
        @if (failureMessage(); as message) {
          <vf-banner tone="error" #failureBanner>{{ message }}</vf-banner>
        }

        <section class="card">
          <h2 class="card-title">{{ t.t('purchaseCreate.section.invoice') }}</h2>
          <div class="grid">
            <vf-form-field [label]="t.t('purchaseCreate.field.supplierName')" [required]="true">
              <vf-text-input [formControl]="form.controls.supplierName" />
            </vf-form-field>
            <vf-form-field [label]="t.t('purchaseCreate.field.supplierReference')">
              <vf-text-input [formControl]="form.controls.supplierInvoiceReference" />
            </vf-form-field>
            <vf-form-field [label]="t.t('purchaseCreate.field.invoiceDate')" [required]="true">
              <vf-date-input [formControl]="form.controls.invoiceDate" />
            </vf-form-field>
          </div>
          <div class="notes">
            <vf-form-field [label]="t.t('purchaseCreate.field.notes')">
              <vf-textarea [formControl]="form.controls.notes" [rows]="3" />
            </vf-form-field>
          </div>
        </section>

        <footer class="actions">
          <vf-button variant="primary" icon="pi-check" type="submit" [disabled]="saving()">
            {{ saving() ? t.t('purchaseCreate.saving') : t.t('purchaseCreate.save') }}
          </vf-button>
          <vf-button variant="quiet" [disabled]="saving()" (pressed)="cancel()">
            {{ t.t('purchaseCreate.cancel') }}
          </vf-button>
        </footer>
      </form>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
      max-inline-size: var(--vf-content-max-width);
      inline-size: 100%;
      margin-inline: auto;
      padding: var(--vf-space-5) var(--vf-space-6);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .form {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
    }

    .card {
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-4) var(--vf-space-5);
    }

    .card-title {
      margin: 0 0 var(--vf-space-3);
      font-size: var(--vf-text-section-title);
      font-weight: 600;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
      gap: var(--vf-space-3);
    }

    .notes {
      margin-block-start: var(--vf-space-3);
    }

    .actions {
      display: flex;
      gap: var(--vf-space-2);
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }
    }
  `,
})
export class PurchaseCreatePageComponent {
  protected readonly t = inject(TranslationService);
  private readonly api = inject(PurchaseCreateApiService);
  private readonly router = inject(Router);
  private readonly mapper = inject(ApiErrorMapper);
  private readonly focus = inject(ValidationFocusService);

  readonly form: PurchaseCreateForm = buildPurchaseCreateForm();

  readonly saving = signal(false);
  /** The classified save failure (ApiErrorMapper output) — null while clean. */
  readonly failure = signal<ClassifiedFailure | null>(null);

  private readonly failureBanner = viewChild('failureBanner', { read: ElementRef });

  protected readonly failureMessage = computed(() => {
    const failure = this.failure();
    return failure ? this.t.t(failure.messageKey, failure.params) : null;
  });

  // The polite live region (STD-UX-092): success navigates (the target screen
  // announces its load) and rejections announce via `role="alert"`, so the
  // in-flight save is this screen's own announceable state.
  protected readonly announcement = computed(() => (this.saving() ? this.t.t('purchaseCreate.saving') : ''));

  /** A server field error means «this value», not a specific rule — the generic
   *  per-field sentence, shown inline on the field itself (STD-UX-019/020). */
  private readonly serverFieldMessages: Readonly<Partial<Record<string, MessageKey>>> = {
    supplierName: 'validation.invalid',
    supplierInvoiceReference: 'validation.invalid',
    invoiceDate: 'validation.invalid',
    notes: 'validation.invalid',
  };

  constructor() {
    // A rejection banner never survives the edit that addresses it (STD-UX-035).
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.failure()) {
        this.failure.set(null);
      }
    });

    // An operation-level rejection has no field target: the banner itself
    // receives focus and is announced (STD-UX-071).
    effect(() => {
      if (!this.failure()) {
        return;
      }

      const banner = this.failureBanner()?.nativeElement;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  /** Runs on `validSubmit` only — the shared guidance already handled the invalid case. */
  protected save(): void {
    const payload = this.buildPayload();
    this.saving.set(true);
    this.failure.set(null);
    this.api.create(payload).subscribe({
      next: (created) => {
        this.saving.set(false);
        void this.router.navigate(['/purchases', created.id]);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        const failure = this.mapper.map(error, { system: 'purchaseCreate.error' });
        if (failure.kind === 'field' && failure.fieldErrors) {
          const unmatched = projectServerFieldErrors(this.form, failure.fieldErrors, this.serverFieldMessages);
          if (unmatched.length === 0) {
            // Every rejected field speaks inline — a banner would say it twice (STD-UX-020).
            return;
          }
        }

        this.failure.set(failure);
      },
    });
  }

  protected cancel(): void {
    void this.router.navigate(['/purchases']);
  }

  /** Empty optional fields serialize as null, never `""` (Details shows «—» for a
   *  missing reference; an empty string would render blank). */
  private buildPayload(): CreatePurchasePayload {
    const raw = this.form.getRawValue();
    return {
      supplierName: raw.supplierName.trim(),
      supplierInvoiceReference: this.nullable(raw.supplierInvoiceReference),
      invoiceDate: raw.invoiceDate ?? '',
      notes: this.nullable(raw.notes),
    };
  }

  private nullable(value: string): string | null {
    return value.trim() === '' ? null : value.trim();
  }
}
