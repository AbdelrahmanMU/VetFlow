import { AbstractControl, FormArray, FormControl, FormGroup, ValidationErrors } from '@angular/forms';

import { vfValidators } from '../../../core/validation/validators';
import { EditProductUnit } from './product-editor.models';

/** One unit-profile row as a typed reactive group (STD-FE-016, BR-CAT-016).
 *  Row validators mirror the server's product-write rules only (STD-UX-021):
 *  conversion factor empty-or-positive (DEC-CAT-009 — user-entered, > 0) and
 *  barcode length, exactly as `ProductWriteCommandValidator` enforces them. */
export type UnitRowForm = FormGroup<{
  unitId: FormControl<string | null>;
  quantityInNextUnit: FormControl<number | null>;
  isPurchaseUnit: FormControl<boolean>;
  isSaleUnit: FormControl<boolean>;
  barcode: FormControl<string>;
  sellingPrice: FormControl<number | null>;
}>;

/** The whole product editor form, shared by create and edit (DEC-CAT-031). */
export type ProductForm = FormGroup<{
  arabicName: FormControl<string>;
  englishName: FormControl<string>;
  size: FormControl<string>;
  concentration: FormControl<string>;
  categoryId: FormControl<string | null>;
  manufacturerId: FormControl<string | null>;
  natureId: FormControl<string | null>;
  isSplittable: FormControl<boolean>;
  isRefrigerated: FormControl<boolean>;
  hasExpiration: FormControl<boolean>;
  hasOpenExpiration: FormControl<boolean>;
  openExpirationPeriodDays: FormControl<number | null>;
  internalNotes: FormControl<string>;
  units: FormArray<UnitRowForm>;
  storageUnitId: FormControl<string | null>;
  defaultSaleUnitId: FormControl<string | null>;
  defaultPurchaseUnitId: FormControl<string | null>;
}>;

/** Server text-length ceilings (`ProductWriteCommandValidator`), mirrored per STD-UX-021. */
const NAME_MAX_LENGTH = 300;
const SHORT_TEXT_MAX_LENGTH = 100;
const NOTES_MAX_LENGTH = 2000;

export function newUnitRow(): UnitRowForm {
  return new FormGroup({
    unitId: new FormControl<string | null>(null, [vfValidators.required]),
    quantityInNextUnit: new FormControl<number | null>(null, [vfValidators.positive]),
    isPurchaseUnit: new FormControl(false, { nonNullable: true }),
    isSaleUnit: new FormControl(false, { nonNullable: true }),
    barcode: new FormControl('', {
      nonNullable: true,
      validators: [vfValidators.maxLength(SHORT_TEXT_MAX_LENGTH)],
    }),
    sellingPrice: new FormControl<number | null>(null),
  });
}

/** A unit row prefilled from an existing product (edit mode, DEC-CAT-031). The
 *  selling price is loaded for read-only display only — it is never submitted. */
export function unitRowFrom(unit: EditProductUnit): UnitRowForm {
  const row = newUnitRow();
  row.controls.unitId.setValue(unit.unitId);
  row.controls.quantityInNextUnit.setValue(unit.quantityInNextUnit);
  row.controls.isPurchaseUnit.setValue(unit.isPurchaseUnit);
  row.controls.isSaleUnit.setValue(unit.isSaleUnit);
  row.controls.barcode.setValue(unit.barcode ?? '');
  row.controls.sellingPrice.setValue(unit.sellingPrice?.amount ?? null);
  return row;
}

/** Cross-row rules of the unit profile (BR-CAT-016/024/025, REQ-CAT-043): a
 *  non-empty profile with at least one purchase and one sale unit. Each rule
 *  keeps its own error key and its own sentence (STD-UX-017). The
 *  open-expiration rule lives on its own control (see
 *  `applyOpenExpirationRule`), field-scoped so blur timing works (§3). */
function productFormValidator(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const errors: ValidationErrors = {};

  const units = group.get('units') as FormArray<UnitRowForm>;
  if (units.length === 0) {
    errors['unitsEmpty'] = true;
  }
  if (units.length > 0 && !units.controls.some((row) => row.controls.isPurchaseUnit.value)) {
    errors['noPurchase'] = true;
  }
  if (units.length > 0 && !units.controls.some((row) => row.controls.isSaleUnit.value)) {
    errors['noSale'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

/**
 * BR-CAT-036 (VTF-CAT-036): «له صلاحية بعد الفتح» requires a positive period.
 * The rule is conditional, so the validators attach only while the capability
 * is on — an off capability clears the value and starts the field fresh.
 * Called from the `hasOpenExpiration` change wiring in the page component.
 */
export function applyOpenExpirationRule(form: ProductForm, enabled: boolean): void {
  const days = form.controls.openExpirationPeriodDays;
  if (enabled) {
    days.setValidators([vfValidators.required, vfValidators.positive]);
  } else {
    days.setValue(null);
    days.clearValidators();
    days.markAsPristine();
    days.markAsUntouched();
  }

  days.updateValueAndValidity();
}

/** Builds the editor form. Create mode starts with two empty unit rows; edit
 *  mode clears and repopulates them from the loaded product. Scalar validators
 *  mirror `ProductWriteCommandValidator` (required minimum per BR-CAT-009,
 *  REQ-CAT-043; length ceilings) — the client predicts, the server decides
 *  (STD-UX-008/021). */
export function buildProductForm(): ProductForm {
  return new FormGroup(
    {
      arabicName: new FormControl('', {
        nonNullable: true,
        validators: [vfValidators.required, vfValidators.maxLength(NAME_MAX_LENGTH)],
      }),
      englishName: new FormControl('', {
        nonNullable: true,
        validators: [vfValidators.maxLength(NAME_MAX_LENGTH)],
      }),
      size: new FormControl('', {
        nonNullable: true,
        validators: [vfValidators.maxLength(SHORT_TEXT_MAX_LENGTH)],
      }),
      concentration: new FormControl('', {
        nonNullable: true,
        validators: [vfValidators.maxLength(SHORT_TEXT_MAX_LENGTH)],
      }),
      categoryId: new FormControl<string | null>(null, [vfValidators.required]),
      manufacturerId: new FormControl<string | null>(null, [vfValidators.required]),
      natureId: new FormControl<string | null>(null, [vfValidators.required]),
      isSplittable: new FormControl(false, { nonNullable: true }),
      isRefrigerated: new FormControl(false, { nonNullable: true }),
      hasExpiration: new FormControl(false, { nonNullable: true }),
      hasOpenExpiration: new FormControl(false, { nonNullable: true }),
      openExpirationPeriodDays: new FormControl<number | null>(null),
      internalNotes: new FormControl('', {
        nonNullable: true,
        validators: [vfValidators.maxLength(NOTES_MAX_LENGTH)],
      }),
      units: new FormArray<UnitRowForm>([newUnitRow(), newUnitRow()]),
      storageUnitId: new FormControl<string | null>(null, [vfValidators.required]),
      defaultSaleUnitId: new FormControl<string | null>(null, [vfValidators.required]),
      defaultPurchaseUnitId: new FormControl<string | null>(null, [vfValidators.required]),
    },
    { validators: [productFormValidator] },
  );
}
