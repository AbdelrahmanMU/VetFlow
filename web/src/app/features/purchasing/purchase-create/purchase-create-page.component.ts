import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../shared/ui-kit/input/vf-date-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { PurchaseCreateApiService } from './purchase-create-api.service';
import { PurchaseCreateForm, buildPurchaseCreateForm } from './purchase-create.forms';
import { CreatePurchasePayload } from './purchase-create.models';

/**
 * شاشة إنشاء فاتورة الشراء (purchasing ui.md, REQ-PUR-003): create a purchase
 * invoice **header** only — supplier name + invoice date required, supplier
 * reference + notes optional. Mirrors the approved Catalog product-editor create
 * path (STD-FE-004 mirror-without-importing): typed reactive form (STD-FE-016),
 * submit → markAllAsTouched → if invalid return; POST → success navigates to the
 * new invoice's Details (`/purchases/:id`, AC-PUR-006); a failure shows a banner.
 * The status is always Draft and the number is generated server-side
 * (BR-PUR-002/003) — no controls; there are no line items or total in this slice
 * (total is 0 server-side, DEC-PUR-002).
 */
@Component({
  selector: 'app-purchase-create-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchaseCreateApiService],
  imports: [ReactiveFormsModule, VfButtonComponent, VfTextInputComponent, VfDateInputComponent, VfTextareaComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('purchaseCreate.title') }}</h1>
      </header>

      @if (banner()) {
        <p class="banner" role="alert">{{ t.t('purchaseCreate.error') }}</p>
      }

      <section class="card">
        <h2 class="card-title">{{ t.t('purchaseCreate.section.invoice') }}</h2>
        <div class="grid">
          <vf-text-input
            [label]="t.t('purchaseCreate.field.supplierName')"
            [required]="true"
            [formControl]="form.controls.supplierName"
            [error]="errorFor(form.controls.supplierName)"
          />
          <vf-text-input
            [label]="t.t('purchaseCreate.field.supplierReference')"
            [formControl]="form.controls.supplierInvoiceReference"
          />
          <vf-date-input
            [label]="t.t('purchaseCreate.field.invoiceDate')"
            [required]="true"
            [value]="form.controls.invoiceDate.value"
            [error]="errorFor(form.controls.invoiceDate)"
            (valueChange)="form.controls.invoiceDate.setValue($event)"
          />
        </div>
        <div class="notes">
          <vf-textarea [label]="t.t('purchaseCreate.field.notes')" [rows]="3" [formControl]="form.controls.notes" />
        </div>
      </section>

      <footer class="actions">
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="submit()">
          {{ saving() ? t.t('purchaseCreate.saving') : t.t('purchaseCreate.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="cancel()">
          {{ t.t('purchaseCreate.cancel') }}
        </vf-button>
      </footer>
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

    .banner {
      margin: 0;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius-small);
      background: var(--vf-danger-soft, #fbeae8);
      color: var(--vf-danger, #b42318);
      font-weight: 500;
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

  readonly form: PurchaseCreateForm = buildPurchaseCreateForm();

  readonly submitted = signal(false);
  readonly saving = signal(false);
  readonly banner = signal(false);

  protected errorFor(control: AbstractControl): string | null {
    return (this.submitted() || control.touched) && control.invalid ? this.t.t('purchaseCreate.required') : null;
  }

  submit(): void {
    this.submitted.set(true);
    this.banner.set(false);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    const payload = this.buildPayload();
    this.saving.set(true);
    this.api.create(payload).subscribe({
      next: (created) => {
        this.saving.set(false);
        void this.router.navigate(['/purchases', created.id]);
      },
      error: () => {
        this.saving.set(false);
        this.banner.set(true);
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
