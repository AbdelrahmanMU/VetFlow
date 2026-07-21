import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfNumberInputComponent } from '../../../../shared/ui-kit/input/vf-number-input.component';
import { VfSelectComponent } from '../../../../shared/ui-kit/select/vf-select.component';
import { PurchaseLinesApiService } from '../purchase-lines-api.service';
import { AddPurchaseLinePayload, ProductPickerOption, PurchaseUnitOption } from '../purchase-lines.models';

/**
 * إضافة بند لفاتورة الشراء (purchasing ui.md, REQ-PUR-004): a presentation dialog over
 * the shared VfDialog — pick an active product, then one of its purchase units
 * (BR-PUR-005, loaded on product change), enter quantity (> 0) and unit price (≥ 0),
 * and see the line-total preview. Validation is field-by-field on submit (AC-PUR-009);
 * the page owns the write (it emits {@link save}). Opening resets the form.
 */
@Component({
  selector: 'app-add-purchase-line-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, VfDialogComponent, VfSelectComponent, VfNumberInputComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('purchaseDetails.lines.dialog.title')" [(visible)]="visible">
      @if (serverError()) {
        <p class="banner" role="alert">{{ t.t('purchaseDetails.lines.dialog.error') }}</p>
      }

      <vf-select
        [label]="t.t('purchaseDetails.lines.field.product')"
        [required]="true"
        [filterable]="true"
        [placeholder]="t.t('purchaseDetails.lines.field.productPlaceholder')"
        [optionList]="productOptions()"
        [value]="selectedProductId()"
        (valueChange)="onProductChange($event)"
        [error]="productError()"
      />

      <vf-select
        [label]="t.t('purchaseDetails.lines.field.unit')"
        [required]="true"
        [placeholder]="t.t('purchaseDetails.lines.field.unitPlaceholder')"
        [optionList]="unitOptions()"
        [value]="selectedUnitId()"
        (valueChange)="selectedUnitId.set($event)"
        [error]="unitError()"
      />

      <div class="row">
        <vf-number-input
          [label]="t.t('purchaseDetails.lines.field.quantity')"
          [required]="true"
          [ngModel]="quantity()"
          (ngModelChange)="quantity.set($event)"
          [error]="quantityError()"
        />
        <vf-number-input
          [label]="t.t('purchaseDetails.lines.field.unitPrice')"
          [required]="true"
          [ngModel]="unitPrice()"
          (ngModelChange)="unitPrice.set($event)"
          [error]="priceError()"
        />
      </div>

      <p class="preview">
        <span>{{ t.t('purchaseDetails.lines.field.lineTotal') }}</span>
        <span class="preview-value vf-num">
          {{ lineTotalPreview() !== null ? format.money(lineTotalPreview()!, currency) : '—' }}
        </span>
      </p>

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onSave()">
          {{ saving() ? t.t('purchaseDetails.lines.dialog.saving') : t.t('purchaseDetails.lines.dialog.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('purchaseDetails.lines.dialog.cancel') }}
        </vf-button>
      </div>
    </vf-dialog>
  `,
  styles: `
    .banner {
      margin: 0;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius-small);
      background: var(--vf-danger-soft, #fbeae8);
      color: var(--vf-danger, #b42318);
      font-weight: 500;
    }

    .row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: var(--vf-space-3);
    }

    .preview {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      gap: var(--vf-space-3);
      margin: 0;
      padding-block-start: var(--vf-space-2);
      color: var(--vf-text-secondary);
    }

    .preview-value {
      font-weight: 600;
      color: var(--vf-text);
    }
  `,
})
export class AddPurchaseLineDialogComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly api = inject(PurchaseLinesApiService);

  readonly visible = model(false);
  readonly products = input<readonly ProductPickerOption[]>([]);
  readonly saving = input(false);
  readonly serverError = input<string | null>(null);
  readonly save = output<AddPurchaseLinePayload>();

  protected readonly currency = 'EGP';

  protected readonly selectedProductId = signal<string | null>(null);
  protected readonly selectedUnitId = signal<string | null>(null);
  protected readonly quantity = signal<number | null>(null);
  protected readonly unitPrice = signal<number | null>(null);
  private readonly purchaseUnits = signal<readonly PurchaseUnitOption[]>([]);
  protected readonly submitted = signal(false);

  protected readonly productOptions = computed(() =>
    this.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly unitOptions = computed(() =>
    this.purchaseUnits().map((unit) => ({ label: unit.unitName, value: unit.unitId })),
  );

  protected readonly lineTotalPreview = computed(() => {
    const quantity = this.quantity();
    const unitPrice = this.unitPrice();
    if (quantity === null || unitPrice === null || quantity <= 0 || unitPrice < 0) {
      return null;
    }

    return Math.round(quantity * unitPrice * 100) / 100;
  });

  protected readonly productError = computed(() =>
    this.submitted() && !this.selectedProductId() ? this.t.t('purchaseDetails.lines.error.product') : null,
  );

  protected readonly unitError = computed(() =>
    this.submitted() && !this.selectedUnitId() ? this.t.t('purchaseDetails.lines.error.unit') : null,
  );

  protected readonly quantityError = computed(() => {
    const quantity = this.quantity();
    return this.submitted() && !(quantity !== null && quantity > 0)
      ? this.t.t('purchaseDetails.lines.error.quantity')
      : null;
  });

  protected readonly priceError = computed(() => {
    const unitPrice = this.unitPrice();
    return this.submitted() && !(unitPrice !== null && unitPrice >= 0)
      ? this.t.t('purchaseDetails.lines.error.price')
      : null;
  });

  constructor() {
    // Reset the form each time the dialog opens (a stale selection never lingers).
    effect(() => {
      if (this.visible()) {
        this.reset();
      }
    });
  }

  protected onProductChange(productId: string | null): void {
    this.selectedProductId.set(productId);
    this.selectedUnitId.set(null);
    this.purchaseUnits.set([]);
    if (!productId) {
      return;
    }

    this.api.getPurchaseUnits(productId).subscribe((units) => {
      this.purchaseUnits.set(units);
      const preferred = units.find((unit) => unit.isDefaultPurchaseUnit) ?? units[0];
      this.selectedUnitId.set(preferred ? preferred.unitId : null);
    });
  }

  protected onSave(): void {
    this.submitted.set(true);
    const productId = this.selectedProductId();
    const purchaseUnitId = this.selectedUnitId();
    const quantity = this.quantity();
    const unitPrice = this.unitPrice();
    if (!productId || !purchaseUnitId || quantity === null || quantity <= 0 || unitPrice === null || unitPrice < 0) {
      return;
    }

    this.save.emit({ productId, purchaseUnitId, quantity, unitPrice });
  }

  private reset(): void {
    this.selectedProductId.set(null);
    this.selectedUnitId.set(null);
    this.purchaseUnits.set([]);
    this.quantity.set(null);
    this.unitPrice.set(null);
    this.submitted.set(false);
  }
}
