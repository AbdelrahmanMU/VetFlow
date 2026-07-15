import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import { EditProductUnit } from './product-editor.models';

/** One unit-profile row as a typed reactive group (STD-FE-016, BR-CAT-016). */
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

export function newUnitRow(): UnitRowForm {
  return new FormGroup({
    unitId: new FormControl<string | null>(null, [Validators.required]),
    quantityInNextUnit: new FormControl<number | null>(null),
    isPurchaseUnit: new FormControl(false, { nonNullable: true }),
    isSaleUnit: new FormControl(false, { nonNullable: true }),
    barcode: new FormControl('', { nonNullable: true }),
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

/** Cross-field rules of the editor (BR-CAT-009/016/024/025): an open-expiration
 *  period when enabled, and at least one purchase and one sale unit. */
function productFormValidator(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const errors: ValidationErrors = {};

  if (group.get('hasOpenExpiration')?.value) {
    const days = group.get('openExpirationPeriodDays')?.value as number | null;
    if (days === null || days <= 0) {
      errors['openExpiration'] = true;
    }
  }

  const units = group.get('units') as FormArray<UnitRowForm>;
  if (units.length === 0) {
    errors['unitsEmpty'] = true;
  }
  if (!units.controls.some((row) => row.controls.isPurchaseUnit.value)) {
    errors['noPurchase'] = true;
  }
  if (!units.controls.some((row) => row.controls.isSaleUnit.value)) {
    errors['noSale'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

/** Builds the editor form. Create mode starts with two empty unit rows; edit
 *  mode clears and repopulates them from the loaded product. */
export function buildProductForm(): ProductForm {
  return new FormGroup(
    {
      arabicName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      englishName: new FormControl('', { nonNullable: true }),
      size: new FormControl('', { nonNullable: true }),
      concentration: new FormControl('', { nonNullable: true }),
      categoryId: new FormControl<string | null>(null, [Validators.required]),
      manufacturerId: new FormControl<string | null>(null, [Validators.required]),
      natureId: new FormControl<string | null>(null, [Validators.required]),
      isSplittable: new FormControl(false, { nonNullable: true }),
      isRefrigerated: new FormControl(false, { nonNullable: true }),
      hasExpiration: new FormControl(false, { nonNullable: true }),
      hasOpenExpiration: new FormControl(false, { nonNullable: true }),
      openExpirationPeriodDays: new FormControl<number | null>(null),
      internalNotes: new FormControl('', { nonNullable: true }),
      units: new FormArray<UnitRowForm>([newUnitRow(), newUnitRow()]),
      storageUnitId: new FormControl<string | null>(null, [Validators.required]),
      defaultSaleUnitId: new FormControl<string | null>(null, [Validators.required]),
      defaultPurchaseUnitId: new FormControl<string | null>(null, [Validators.required]),
    },
    { validators: [productFormValidator] },
  );
}
