import { FormControl, FormGroup, Validators } from '@angular/forms';

/** One unit-profile row as a typed reactive group (STD-FE-016, BR-CAT-016). */
export type UnitRowForm = FormGroup<{
  unitId: FormControl<string | null>;
  quantityInNextUnit: FormControl<number | null>;
  isPurchaseUnit: FormControl<boolean>;
  isSaleUnit: FormControl<boolean>;
  barcode: FormControl<string>;
  sellingPrice: FormControl<number | null>;
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
