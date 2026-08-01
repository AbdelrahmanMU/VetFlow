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
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { SubmitGuidanceDirective } from '../../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../../core/validation/validation-focus.service';
import { RuleMessageOverrides } from '../../../../core/validation/validation-messages';
import { vfValidators } from '../../../../core/validation/validators';
import { VfBannerComponent } from '../../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfFormFieldComponent } from '../../../../shared/ui-kit/form-field/vf-form-field.component';
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
 *   product rejects a fractional quantity with its own field message (the shared `wholeNumber`
 *   validator, armed only when the profile says so). Nothing is rounded or corrected.
 *
 * Nothing here mentions batches — no picker, no expiry, no FEFO hint (BR-SAL-013).
 *
 * Validation-foundation adoption (validation-and-guidance.md, Adoption Epic):
 * three moments through `vf-form-field` with the ruled per-field wordings
 * (STD-UX-111); shared submit guidance (moment 3 + first-invalid focus,
 * STD-UX-012/070); classified failures in the shared banner, focused on
 * appearance (STD-UX-071); a failed profile load surfaced with retry
 * (STD-UX-041).
 */
@Component({
  selector: 'app-add-sale-line-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfDialogComponent,
    VfFormFieldComponent,
    VfSelectComponent,
    VfNumberInputComponent,
    VfButtonComponent,
  ],
  template: `
    <vf-dialog [header]="t.t('saleDetails.lines.dialog.title')" [(visible)]="visible">
      <form [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="emitSave()">
        @if (operationError(); as message) {
          <vf-banner tone="error" #operationBanner>{{ message }}</vf-banner>
        }
        @if (productsLoadFailed()) {
          <vf-banner tone="error">
            {{ t.t('pickers.productsError') }}
            <button type="button" class="retry-link" (click)="retryProducts.emit()">
              {{ t.t('errors.retry') }}
            </button>
          </vf-banner>
        }

        <vf-form-field [label]="t.t('saleDetails.lines.field.product')" [required]="true" [messages]="productMessages">
          <vf-select
            [formControl]="form.controls.productId"
            [filterable]="true"
            [placeholder]="t.t('saleDetails.lines.field.productPlaceholder')"
            [optionList]="productOptions()"
          />
        </vf-form-field>

        <vf-form-field [label]="t.t('saleDetails.lines.field.unit')" [required]="true" [messages]="unitMessages">
          <vf-select
            [formControl]="form.controls.unitId"
            [placeholder]="t.t('saleDetails.lines.field.unitPlaceholder')"
            [optionList]="unitOptions()"
          />
        </vf-form-field>
        @if (unitsError()) {
          <vf-banner tone="error">
            {{ t.t('pickers.unitsError') }}
            <button type="button" class="retry-link" (click)="loadProfile(form.controls.productId.value)">
              {{ t.t('errors.retry') }}
            </button>
          </vf-banner>
        }

        <vf-form-field [label]="t.t('saleDetails.lines.field.quantity')" [required]="true" [messages]="quantityMessages">
          <vf-number-input [formControl]="form.controls.quantity" />
        </vf-form-field>

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
      </form>

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
    form {
      display: flex;
      flex-direction: column;
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

    .retry-link {
      border: none;
      background: none;
      padding: 0;
      color: inherit;
      font: inherit;
      font-weight: 600;
      text-decoration: underline;
      cursor: pointer;
    }
  `,
})
export class AddSaleLineDialogComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly api = inject(SaleLinesApiService);
  private readonly focus = inject(ValidationFocusService);

  readonly visible = model(false);
  readonly products = input<readonly ProductPickerOption[]>([]);
  readonly saving = input(false);
  /** The page's classified save failure (ApiErrorMapper output) — null while clean. */
  readonly serverFailure = input<ClassifiedFailure | null>(null);
  /** The page's product-picker load failed — surfaced here with a retry (STD-UX-041). */
  readonly productsLoadFailed = input(false);
  readonly save = output<AddSaleLinePayload>();
  readonly retryProducts = output<void>();

  protected readonly currency = 'EGP';

  // The documented line rules only (STD-UX-021): product + unit + quantity
  // required; quantity > 0; whole quantity for a non-splittable product
  // (DEC-SAL-007 / BR-CAT-032) — armed per profile below.
  protected readonly form = new FormGroup({
    productId: new FormControl<string | null>(null, vfValidators.required),
    unitId: new FormControl<string | null>(null, vfValidators.required),
    quantity: new FormControl<number | null>(null, [vfValidators.required, vfValidators.positive]),
  });

  // The ruled per-field wordings (sales ui.md) as overrides (STD-UX-111) —
  // splittability keeps its own sentence (STD-UX-017).
  protected readonly productMessages: RuleMessageOverrides = { required: 'saleDetails.lines.error.product' };
  protected readonly unitMessages: RuleMessageOverrides = { required: 'saleDetails.lines.error.unit' };
  protected readonly quantityMessages: RuleMessageOverrides = {
    required: 'saleDetails.lines.error.quantity',
    positive: 'saleDetails.lines.error.quantity',
    wholeNumber: 'saleDetails.lines.error.notSplittable',
  };

  protected readonly operationError = signal<string | null>(null);
  protected readonly unitsError = signal(false);
  private readonly saleUnits = signal<readonly SaleUnitOption[]>([]);

  private readonly guide = viewChild(SubmitGuidanceDirective);
  private readonly operationBanner = viewChild('operationBanner', { read: ElementRef });

  protected readonly productOptions = computed(() =>
    this.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly unitOptions = computed(() =>
    this.saleUnits().map((unit) => ({ label: unit.unitName, value: unit.unitId })),
  );

  /** Signal view of the form value, so the previews stay reactive under OnPush. */
  private readonly formValue = signal<{ unitId: string | null; quantity: number | null }>({
    unitId: null,
    quantity: null,
  });

  /** The catalog price of the selected unit — «—» when the catalog defines none (the server rejects such a line). */
  protected readonly selectedPrice = computed(
    () => this.saleUnits().find((unit) => unit.unitId === this.formValue().unitId)?.sellingPrice ?? null,
  );

  protected readonly lineTotalPreview = computed(() => {
    const quantity = this.formValue().quantity;
    const price = this.selectedPrice();
    if (quantity === null || quantity <= 0 || !price) {
      return null;
    }

    return Math.round(quantity * price.amount * 100) / 100;
  });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.formValue.set({ unitId: value.unitId ?? null, quantity: value.quantity ?? null });
    });

    // The unit picker follows the chosen product; changing it always clears
    // the dependent choice and re-reads the sale profile.
    this.form.controls.productId.valueChanges.pipe(takeUntilDestroyed()).subscribe((productId) => {
      this.form.controls.unitId.setValue(null);
      this.loadProfile(productId);
    });

    // Reset the form each time the dialog opens (a stale selection never
    // lingers): a fresh moment cycle (STD-UX-014).
    effect(() => {
      if (this.visible()) {
        this.reset();
      }
    });

    // The page's classified failure renders inside the dialog (STD-UX-082) —
    // add-line has no ruled field projection, so every failure is an
    // operation message here.
    effect(() => {
      const failure = this.serverFailure();
      this.operationError.set(failure ? this.t.t(failure.messageKey, failure.params) : null);
    });

    // The operation message receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.operationError()) {
        return;
      }

      const banner = this.operationBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  protected loadProfile(productId: string | null): void {
    this.saleUnits.set([]);
    this.unitsError.set(false);
    this.armSplittability(true);
    if (!productId) {
      return;
    }

    this.api.getSaleProfile(productId).subscribe({
      next: (profile) => {
        this.saleUnits.set(profile.units);
        // Explicit rejection of fractional quantities for a non-splittable
        // product, never a silent correction (DEC-SAL-007) — the shared
        // wholeNumber validator carries the rule (STD-UX-022/125).
        this.armSplittability(profile.isSplittable);
        // The product's default sale unit wins; otherwise the first sale unit (BR-CAT-022).
        const preferred = profile.units.find((unit) => unit.isDefaultSaleUnit) ?? profile.units[0];
        this.form.controls.unitId.setValue(preferred ? preferred.unitId : null);
      },
      // A failed load is surfaced with a retry — never a silently empty picker (STD-UX-041).
      error: () => this.unitsError.set(true),
    });
  }

  private armSplittability(isSplittable: boolean): void {
    const quantity = this.form.controls.quantity;
    quantity.setValidators(
      isSplittable
        ? [vfValidators.required, vfValidators.positive]
        : [vfValidators.required, vfValidators.positive, vfValidators.wholeNumber],
    );
    quantity.updateValueAndValidity();
  }

  /** The footer button lives outside the form element, so it triggers the shared guidance. */
  protected onSave(): void {
    this.guide()?.trigger();
  }

  protected emitSave(): void {
    const { productId, unitId, quantity } = this.form.getRawValue();
    // validSubmit fires only on a valid form; the guard keeps types honest (STD-FE-022).
    if (!productId || !unitId || quantity === null) {
      return;
    }

    this.save.emit({ productId, saleUnitId: unitId, quantity });
  }

  private reset(): void {
    this.form.reset({ productId: null, unitId: null, quantity: null });
    this.saleUnits.set([]);
    this.unitsError.set(false);
    this.operationError.set(null);
    this.armSplittability(true);
    this.guide()?.resetSubmitted();
  }
}
