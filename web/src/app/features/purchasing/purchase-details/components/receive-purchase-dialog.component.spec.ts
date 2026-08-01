import { TestBed } from '@angular/core/testing';
import { FormControl, FormRecord } from '@angular/forms';

import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { PurchaseLine, ReceivePurchaseInvoicePayload } from '../purchase-lines.models';
import { ReceivePurchaseDialogComponent } from './receive-purchase-dialog.component';

/**
 * Receive confirmation dialog (REQ-PUR-005, BR-PUR-013): shows a required expiry input only for lines
 * whose product requires expiry, blocks confirmation until every required date is set, and emits the
 * per-line expiry payload (DEC-PUR-009). On the validation foundation: the per-line requirement runs
 * through `vf-form-field` (STD-UX-084) and a classified rejection renders per-code (STD-UX-037) with
 * the retry relabel on a retryable conflict (STD-UX-033).
 */
interface DialogInternals {
  onConfirm(): void;
  form: FormRecord<FormControl<string | null>>;
}

describe('ReceivePurchaseDialogComponent', () => {
  const expiryLine: PurchaseLine = {
    id: 'l1',
    productId: 'p1',
    productName: 'لقاح',
    purchaseUnitId: 'u1',
    purchaseUnitName: 'كرتونة',
    quantity: 1,
    unitPrice: { amount: 100, currency: 'EGP' },
    lineTotal: { amount: 100, currency: 'EGP' },
    requiresExpiry: true,
  };
  const plainLine: PurchaseLine = { ...expiryLine, id: 'l2', productName: 'شاش', requiresExpiry: false };

  function setup(lines: PurchaseLine[]) {
    TestBed.configureTestingModule({});
    const fixture = TestBed.createComponent(ReceivePurchaseDialogComponent);
    fixture.componentRef.setInput('lines', lines);
    fixture.componentRef.setInput('visible', true);
    TestBed.tick();
    fixture.detectChanges();
    return fixture;
  }

  it('shows an expiry input only for lines whose product requires expiry (BR-PUR-013)', () => {
    const element: HTMLElement = setup([expiryLine, plainLine]).nativeElement;

    expect(element.querySelectorAll('vf-date-input').length).toBe(1);
    expect(element.textContent).toContain('لقاح');
  });

  it('blocks confirmation with the per-line inline error until the date is set, then emits (AC-PUR-018, STD-UX-084)', () => {
    const fixture = setup([expiryLine]);
    const internals = fixture.componentInstance as unknown as DialogInternals;
    const emitted: ReceivePurchaseInvoicePayload[] = [];
    fixture.componentInstance.confirmed.subscribe((payload) => emitted.push(payload));

    internals.onConfirm(); // no date yet → blocked
    fixture.detectChanges();
    expect(emitted.length).toBe(0);
    // The offending line shows its own inline message — not one sentence for N lines.
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('تاريخ الصلاحية مطلوب');

    internals.form.controls['l1'].setValue('2027-03-01');
    internals.onConfirm();

    expect(emitted).toEqual([{ lines: [{ lineId: 'l1', expiryDate: '2027-03-01' }] }]);
  });

  it('emits an empty lines payload when no product requires expiry', () => {
    const fixture = setup([plainLine]);
    const emitted: ReceivePurchaseInvoicePayload[] = [];
    fixture.componentInstance.confirmed.subscribe((payload) => emitted.push(payload));

    (fixture.componentInstance as unknown as DialogInternals).onConfirm();

    expect(emitted).toEqual([{ lines: [] }]);
  });

  it('a classified rejection renders per-code and relabels the confirm to retry when retryable (STD-UX-037/033)', () => {
    const fixture = setup([plainLine]);

    fixture.componentRef.setInput('serverFailure', {
      kind: 'business',
      code: 'VTF-PUR-006',
      messageKey: 'errors.VTF-PUR-006',
      retryable: false,
      fieldErrors: null,
    } satisfies ClassifiedFailure);
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
    expect(banner?.textContent).toContain('لا يمكن استلام فاتورة شراء بلا بنود');
    expect(banner?.getAttribute('role')).toBe('alert');

    fixture.componentRef.setInput('serverFailure', {
      kind: 'concurrency',
      code: 'VTF-INV-068',
      messageKey: 'errors.VTF-INV-068',
      retryable: true,
      fieldErrors: null,
    } satisfies ClassifiedFailure);
    fixture.detectChanges();

    const primary = (fixture.nativeElement as HTMLElement).querySelector('.vf-button--primary');
    expect(primary?.textContent).toContain('إعادة المحاولة');
  });
});
