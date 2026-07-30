import { FormControl, FormGroup, Validators } from '@angular/forms';

/**
 * The create-sale header form (STD-FE-016, REQ-SAL-001 / BR-SAL-001): the sale date is the only
 * required field — the customer name is optional by owner ruling (DEC-SAL-002), as are the notes.
 * Status and the system number are not user input (BR-SAL-002/003) and have no controls.
 */
export type SaleCreateForm = FormGroup<{
  customerName: FormControl<string>;
  saleDate: FormControl<string | null>;
  notes: FormControl<string>;
}>;

export function buildSaleCreateForm(): SaleCreateForm {
  return new FormGroup({
    customerName: new FormControl('', { nonNullable: true }),
    saleDate: new FormControl<string | null>(null, [Validators.required]),
    notes: new FormControl('', { nonNullable: true }),
  });
}
