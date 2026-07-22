import { TestBed } from '@angular/core/testing';

import { PurchaseLine, ReceivePurchaseInvoicePayload } from '../purchase-lines.models';
import { ReceivePurchaseDialogComponent } from './receive-purchase-dialog.component';

/**
 * Receive confirmation dialog (REQ-PUR-005, BR-PUR-013): shows a required expiry input only for lines
 * whose product requires expiry, blocks confirmation until every required date is set, and emits the
 * per-line expiry payload (DEC-PUR-009).
 */
interface DialogInternals {
  onConfirm(): void;
  setExpiry(lineId: string, value: string | null): void;
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

  it('blocks confirmation until the required expiry is set, then emits the payload (AC-PUR-018)', () => {
    const fixture = setup([expiryLine]);
    const internals = fixture.componentInstance as unknown as DialogInternals;
    const emitted: ReceivePurchaseInvoicePayload[] = [];
    fixture.componentInstance.confirmed.subscribe((payload) => emitted.push(payload));

    internals.onConfirm(); // no date yet → blocked
    expect(emitted.length).toBe(0);

    internals.setExpiry('l1', '2027-03-01');
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
});
