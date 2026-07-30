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
import { SaleLinesApiService } from '../sale-lines-api.service';
import { AddSaleLinePayload, ProductPickerOption, SaleUnitOption } from '../sale-lines.models';

/**
 * إضافة بند لفاتورة البيع (sales ui.md, REQ-SAL-001): a presentation dialog over the shared
 * VfDialog, mirroring the approved add-purchase-line dialog — pick an active product, then one of
 * its **sale** units (loaded on product change, the default one auto-selected — BR-CAT-022), enter
 * the quantity, and see the line-total preview. The page owns the write (it emits {@link save}).
 * Opening resets the form.
 *
 * Two deliberate differences from the purchasing dialog:
 * - **The price is read-only** (DEC-SAL-003): it is displayed from the catalog and never entered,
 *   and the payload carries no price — the server snapshots it (BR-SAL-006). There is no discount,
 *   no alternative price and no reason field.
 * - **Splittability is enforced** (DEC-SAL-007 over the existing BR-CAT-032): a non-splittable
 *   product rejects a fractional quantity with a field message. Nothing is rounded or corrected.
 *
 * Nothing here mentions batches — no picker, no expiry, no FEFO hint (BR-SAL-013).
 */
@Component({
  selector: 'app-add-sale-line-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, VfDialogComponent, VfSelectComponent, VfNumberInputComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="t.t('saleDetails.lines.dialog.title')" [(visible)]="visible">
      @if (serverError()) {
        <p class="banner" role="alert">{{ serverError() }}</p>
      }

      <vf-select
        [label]="t.t('saleDetails.lines.field.product')"
        [required]="true"
        [filterable]="true"
        [placeholder]="t.t('saleDetails.lines.field.productPlaceholder')"
        [optionList]="productOptions()"
        [value]="selectedProductId()"
        (valueChange)="onProductChange($event)"
        [error]="productError()"
      />

      <vf-select
        [label]="t.t('saleDetails.lines.field.unit')"
        [required]="true"
        [placeholder]="t.t('saleDetails.lines.field.unitPlaceholder')"
        [optionList]="unitOptions()"
        [value]="selectedUnitId()"
        (valueChange)="selectedUnitId.set($event)"
        [error]="unitError()"
      />

      <vf-number-input
        [label]="t.t('saleDetails.lines.field.quantity')"
        [required]="true"
        [ngModel]="quantity()"
        (ngModelChange)="quantity.set($event)"
        [error]="quantityError()"
      />

      <!-- Read-only: the sale price is shown, never entered (DEC-SAL-003). -->
      <p class="preview">
        <span>{{ t.t('saleDetails.lines.field.unitPrice') }}</span>
        <span class="preview-value vf-num">
          {{ selectedPrice() ? format.money(selectedPrice()!.amount, selectedPrice()!.currency) : '—' }}
        </span>
      </p>

      <p class="preview">
        <span>{{ t.t('saleDetails.lines.field.lineTotal') }}</span>
        <span class="preview-value vf-num">
          {{ lineTotalPreview() !== null ? format.money(lineTotalPreview()!, currency) : '—' }}
        </span>
      </p>

      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onSave()">
          {{ saving() ? t.t('saleDetails.lines.dialog.saving') : t.t('saleDetails.lines.dialog.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('saleDetails.lines.dialog.cancel') }}
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
export class AddSaleLineDialogComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly api = inject(SaleLinesApiService);

  readonly visible = model(false);
  readonly products = input<readonly ProductPickerOption[]>([]);
  readonly saving = input(false);
  readonly serverError = input<string | null>(null);
  readonly save = output<AddSaleLinePayload>();

  protected readonly currency = 'EGP';

  protected readonly selectedProductId = signal<string | null>(null);
  protected readonly selectedUnitId = signal<string | null>(null);
  protected readonly quantity = signal<number | null>(null);
  private readonly saleUnits = signal<readonly SaleUnitOption[]>([]);
  /** Whether the picked product may be sold in fractions (DEC-SAL-007); unknown products stay permissive. */
  private readonly isSplittable = signal(true);
  protected readonly submitted = signal(false);

  protected readonly productOptions = computed(() =>
    this.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly unitOptions = computed(() =>
    this.saleUnits().map((unit) => ({ label: unit.unitName, value: unit.unitId })),
  );

  /** The catalog price of the selected unit — «—» when the catalog defines none (the server rejects such a line). */
  protected readonly selectedPrice = computed(
    () => this.saleUnits().find((unit) => unit.unitId === this.selectedUnitId())?.sellingPrice ?? null,
  );

  protected readonly lineTotalPreview = computed(() => {
    const quantity = this.quantity();
    const price = this.selectedPrice();
    if (quantity === null || quantity <= 0 || !price) {
      return null;
    }

    return Math.round(quantity * price.amount * 100) / 100;
  });

  protected readonly productError = computed(() =>
    this.submitted() && !this.selectedProductId() ? this.t.t('saleDetails.lines.error.product') : null,
  );

  protected readonly unitError = computed(() =>
    this.submitted() && !this.selectedUnitId() ? this.t.t('saleDetails.lines.error.unit') : null,
  );

  protected readonly quantityError = computed(() => {
    if (!this.submitted()) {
      return null;
    }

    const quantity = this.quantity();
    if (quantity === null || quantity <= 0) {
      return this.t.t('saleDetails.lines.error.quantity');
    }

    // Explicit rejection, never a silent correction (DEC-SAL-007).
    return !this.isSplittable() && !Number.isInteger(quantity)
      ? this.t.t('saleDetails.lines.error.notSplittable')
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
    this.saleUnits.set([]);
    this.isSplittable.set(true);
    if (!productId) {
      return;
    }

    this.api.getSaleProfile(productId).subscribe((profile) => {
      this.saleUnits.set(profile.units);
      this.isSplittable.set(profile.isSplittable);
      // The product's default sale unit wins; otherwise the first sale unit (BR-CAT-022).
      const preferred = profile.units.find((unit) => unit.isDefaultSaleUnit) ?? profile.units[0];
      this.selectedUnitId.set(preferred ? preferred.unitId : null);
    });
  }

  protected onSave(): void {
    this.submitted.set(true);
    const productId = this.selectedProductId();
    const saleUnitId = this.selectedUnitId();
    const quantity = this.quantity();
    if (!productId || !saleUnitId || quantity === null || quantity <= 0) {
      return;
    }

    if (!this.isSplittable() && !Number.isInteger(quantity)) {
      return;
    }

    this.save.emit({ productId, saleUnitId, quantity });
  }

  private reset(): void {
    this.selectedProductId.set(null);
    this.selectedUnitId.set(null);
    this.saleUnits.set([]);
    this.isSplittable.set(true);
    this.quantity.set(null);
    this.submitted.set(false);
  }
}
