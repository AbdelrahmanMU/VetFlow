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
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors } from '@angular/forms';

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
import { PurchaseLinesApiService } from '../purchase-lines-api.service';
import { AddPurchaseLinePayload, ProductPickerOption, PurchaseUnitOption } from '../purchase-lines.models';

/**
 * Unit price ≥ 0 (BR-PUR-005 — a purchase line's price may be zero, e.g. a
 * bonus quantity, never negative). Screen-local because the frozen shared
 * library (Foundation v1) has no non-negative shape; recorded as debt for the
 * next Foundation window (STD-UX-125 exception, Adoption Epic report).
 */
function nonNegative(control: AbstractControl): ValidationErrors | null {
  const value: unknown = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }

  return typeof value === 'number' && value >= 0 ? null : { nonNegative: true };
}

/**
 * إضافة بند لفاتورة الشراء (purchasing ui.md, REQ-PUR-004): a presentation dialog over
 * the shared VfDialog — pick an active product, then one of its purchase units
 * (BR-PUR-005, loaded on product change), enter quantity (> 0) and unit price (≥ 0),
 * and see the line-total preview. The page owns the write (it emits {@link save}).
 *
 * Validation-foundation adoption (validation-and-guidance.md, Adoption Epic):
 * the fields run the three moments through `vf-form-field` with the ruled
 * per-field wordings (AC-PUR-009, STD-UX-111); submit runs the shared
 * guidance (moment 3 + first-invalid focus, STD-UX-012/070); a classified
 * failure renders in the shared banner inside the dialog (STD-UX-121/080),
 * focused on appearance (STD-UX-071); a failed units load is surfaced with a
 * retry, never a silently empty picker (STD-UX-041). Opening resets the form.
 */
@Component({
  selector: 'app-add-purchase-line-dialog',
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
    <vf-dialog [header]="t.t('purchaseDetails.lines.dialog.title')" [(visible)]="visible">
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

        <vf-form-field
          [label]="t.t('purchaseDetails.lines.field.product')"
          [required]="true"
          [messages]="productMessages"
        >
          <vf-select
            [formControl]="form.controls.productId"
            [filterable]="true"
            [placeholder]="t.t('purchaseDetails.lines.field.productPlaceholder')"
            [optionList]="productOptions()"
          />
        </vf-form-field>

        <vf-form-field [label]="t.t('purchaseDetails.lines.field.unit')" [required]="true" [messages]="unitMessages">
          <vf-select
            [formControl]="form.controls.unitId"
            [placeholder]="t.t('purchaseDetails.lines.field.unitPlaceholder')"
            [optionList]="unitOptions()"
          />
        </vf-form-field>
        @if (unitsError()) {
          <vf-banner tone="error">
            {{ t.t('pickers.unitsError') }}
            <button type="button" class="retry-link" (click)="loadUnits(form.controls.productId.value)">
              {{ t.t('errors.retry') }}
            </button>
          </vf-banner>
        }

        <div class="row">
          <vf-form-field
            [label]="t.t('purchaseDetails.lines.field.quantity')"
            [required]="true"
            [messages]="quantityMessages"
          >
            <vf-number-input [formControl]="form.controls.quantity" />
          </vf-form-field>
          <vf-form-field
            [label]="t.t('purchaseDetails.lines.field.unitPrice')"
            [required]="true"
            [messages]="priceMessages"
          >
            <vf-number-input [formControl]="form.controls.unitPrice" />
          </vf-form-field>
        </div>

        <p class="preview">
          <span>{{ t.t('purchaseDetails.lines.field.lineTotal') }}</span>
          <span class="preview-value vf-num">
            {{ lineTotalPreview() !== null ? format.money(lineTotalPreview()!, currency) : '—' }}
          </span>
        </p>
      </form>

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
    form {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
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
export class AddPurchaseLineDialogComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  private readonly api = inject(PurchaseLinesApiService);
  private readonly focus = inject(ValidationFocusService);

  readonly visible = model(false);
  readonly products = input<readonly ProductPickerOption[]>([]);
  readonly saving = input(false);
  /** The page's classified save failure (ApiErrorMapper output) — null while clean. */
  readonly serverFailure = input<ClassifiedFailure | null>(null);
  /** The page's product-picker load failed — surfaced here with a retry (STD-UX-041). */
  readonly productsLoadFailed = input(false);
  readonly save = output<AddPurchaseLinePayload>();
  readonly retryProducts = output<void>();

  protected readonly currency = 'EGP';

  // The documented line rules only (STD-UX-021): product + unit + quantity +
  // price required (AC-PUR-009); quantity > 0 and price ≥ 0 (BR-PUR-005).
  protected readonly form = new FormGroup({
    productId: new FormControl<string | null>(null, vfValidators.required),
    unitId: new FormControl<string | null>(null, vfValidators.required),
    quantity: new FormControl<number | null>(null, [vfValidators.required, vfValidators.positive]),
    unitPrice: new FormControl<number | null>(null, [vfValidators.required, nonNegative]),
  });

  // The ruled per-field wordings (purchasing ui.md) as overrides (STD-UX-111).
  protected readonly productMessages: RuleMessageOverrides = { required: 'purchaseDetails.lines.error.product' };
  protected readonly unitMessages: RuleMessageOverrides = { required: 'purchaseDetails.lines.error.unit' };
  protected readonly quantityMessages: RuleMessageOverrides = {
    required: 'purchaseDetails.lines.error.quantity',
    positive: 'purchaseDetails.lines.error.quantity',
  };
  protected readonly priceMessages: RuleMessageOverrides = {
    required: 'purchaseDetails.lines.error.price',
    nonNegative: 'purchaseDetails.lines.error.price',
  };

  protected readonly operationError = signal<string | null>(null);
  protected readonly unitsError = signal(false);
  private readonly purchaseUnits = signal<readonly PurchaseUnitOption[]>([]);

  private readonly guide = viewChild(SubmitGuidanceDirective);
  private readonly operationBanner = viewChild('operationBanner', { read: ElementRef });

  protected readonly productOptions = computed(() =>
    this.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly unitOptions = computed(() =>
    this.purchaseUnits().map((unit) => ({ label: unit.unitName, value: unit.unitId })),
  );

  protected readonly lineTotalPreview = computed(() => {
    const { quantity, unitPrice } = this.formValue();
    if (quantity === null || unitPrice === null || quantity <= 0 || unitPrice < 0) {
      return null;
    }

    return Math.round(quantity * unitPrice * 100) / 100;
  });

  /** Signal view of the form value, so the preview stays reactive under OnPush. */
  private readonly formValue = signal<{ quantity: number | null; unitPrice: number | null }>({
    quantity: null,
    unitPrice: null,
  });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.formValue.set({ quantity: value.quantity ?? null, unitPrice: value.unitPrice ?? null });
    });

    // The unit picker follows the chosen product; changing it always clears
    // the dependent choice (BR-PUR-005: units come from the product's profile).
    this.form.controls.productId.valueChanges.pipe(takeUntilDestroyed()).subscribe((productId) => {
      this.form.controls.unitId.setValue(null);
      this.loadUnits(productId);
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

  protected loadUnits(productId: string | null): void {
    this.purchaseUnits.set([]);
    this.unitsError.set(false);
    if (!productId) {
      return;
    }

    this.api.getPurchaseUnits(productId).subscribe({
      next: (units) => {
        this.purchaseUnits.set(units);
        const preferred = units.find((unit) => unit.isDefaultPurchaseUnit) ?? units[0];
        this.form.controls.unitId.setValue(preferred ? preferred.unitId : null);
      },
      // A failed load is surfaced with a retry — never a silently empty picker (STD-UX-041).
      error: () => this.unitsError.set(true),
    });
  }

  /** The footer button lives outside the form element, so it triggers the shared guidance. */
  protected onSave(): void {
    this.guide()?.trigger();
  }

  protected emitSave(): void {
    const { productId, unitId, quantity, unitPrice } = this.form.getRawValue();
    // validSubmit fires only on a valid form; the guard keeps types honest (STD-FE-022).
    if (!productId || !unitId || quantity === null || unitPrice === null) {
      return;
    }

    this.save.emit({ productId, purchaseUnitId: unitId, quantity, unitPrice });
  }

  private reset(): void {
    this.form.reset({ productId: null, unitId: null, quantity: null, unitPrice: null });
    this.purchaseUnits.set([]);
    this.unitsError.set(false);
    this.operationError.set(null);
    this.guide()?.resetSubmitted();
  }
}
