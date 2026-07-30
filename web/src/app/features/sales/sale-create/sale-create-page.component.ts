import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfDateInputComponent } from '../../../shared/ui-kit/input/vf-date-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { SaleCreateApiService } from './sale-create-api.service';
import { SaleCreateForm, buildSaleCreateForm } from './sale-create.forms';
import { CreateSalePayload } from './sale-create.models';

/**
 * شاشة إنشاء فاتورة بيع (sales ui.md, REQ-SAL-001): creates the **header** only — the sale date is
 * required, the customer name and notes are optional (DEC-SAL-002). A literal mirror of the
 * approved create-purchase screen (STD-FE-004 mirror-without-importing): typed reactive form
 * (STD-FE-016), submit → markAllAsTouched → if invalid return; POST → success navigates to the new
 * invoice's details (`/sales/:id`); a failure shows a banner. The status is always Draft and the
 * number is generated server-side (BR-SAL-002/003) — no controls. **A draft touches no inventory**
 * (BR-SAL-010).
 */
@Component({
  selector: 'app-sale-create-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SaleCreateApiService],
  imports: [ReactiveFormsModule, VfButtonComponent, VfTextInputComponent, VfDateInputComponent, VfTextareaComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('saleCreate.title') }}</h1>
      </header>

      @if (banner()) {
        <p class="banner" role="alert">{{ t.t('saleCreate.error') }}</p>
      }

      <section class="card">
        <h2 class="card-title">{{ t.t('saleCreate.section.invoice') }}</h2>
        <div class="grid">
          <vf-text-input [label]="t.t('saleCreate.field.customerName')" [formControl]="form.controls.customerName" />
          <vf-date-input
            [label]="t.t('saleCreate.field.saleDate')"
            [required]="true"
            [value]="form.controls.saleDate.value"
            [error]="errorFor(form.controls.saleDate)"
            (valueChange)="form.controls.saleDate.setValue($event)"
          />
        </div>
        <div class="notes">
          <vf-textarea [label]="t.t('saleCreate.field.notes')" [rows]="3" [formControl]="form.controls.notes" />
        </div>
      </section>

      <footer class="actions">
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="submit()">
          {{ saving() ? t.t('saleCreate.saving') : t.t('saleCreate.save') }}
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
export class SaleCreatePageComponent {
  protected readonly t = inject(TranslationService);
  private readonly api = inject(SaleCreateApiService);
  private readonly router = inject(Router);

  readonly form: SaleCreateForm = buildSaleCreateForm();

  readonly submitted = signal(false);
  readonly saving = signal(false);
  readonly banner = signal(false);

  protected errorFor(control: AbstractControl): string | null {
    return (this.submitted() || control.touched) && control.invalid ? this.t.t('saleCreate.required') : null;
  }

  submit(): void {
    this.submitted.set(true);
    this.banner.set(false);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    this.saving.set(true);
    this.api.create(this.buildPayload()).subscribe({
      next: (created) => {
        this.saving.set(false);
        void this.router.navigate(['/sales', created.id]);
      },
      error: () => {
        this.saving.set(false);
        this.banner.set(true);
      },
    });
  }

  /** Empty optional fields serialize as null, never `""` — details shows «—» for a missing value. */
  private buildPayload(): CreateSalePayload {
    const raw = this.form.getRawValue();
    return {
      customerName: this.nullable(raw.customerName),
      saleDate: raw.saleDate ?? '',
      notes: this.nullable(raw.notes),
    };
  }

  private nullable(value: string): string | null {
    return value.trim() === '' ? null : value.trim();
  }
}
