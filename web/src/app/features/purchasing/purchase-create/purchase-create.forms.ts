import { FormControl, FormGroup } from '@angular/forms';

import { vfValidators } from '../../../core/validation/validators';

/**
 * The create-purchase header form (STD-FE-016, REQ-PUR-003 / BR-PUR-001): supplier
 * name and invoice date are required; the supplier reference and notes are
 * optional. Status and the system number are not user input (BR-PUR-002/003) and
 * have no controls; there are no line items or total in this slice.
 */
export type PurchaseCreateForm = FormGroup<{
  supplierName: FormControl<string>;
  supplierInvoiceReference: FormControl<string>;
  invoiceDate: FormControl<string | null>;
  notes: FormControl<string>;
}>;

export function buildPurchaseCreateForm(): PurchaseCreateForm {
  return new FormGroup({
    supplierName: new FormControl('', { nonNullable: true, validators: [vfValidators.required] }),
    supplierInvoiceReference: new FormControl('', { nonNullable: true }),
    invoiceDate: new FormControl<string | null>(null, [vfValidators.required]),
    notes: new FormControl('', { nonNullable: true }),
  });
}
