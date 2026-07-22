import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PurchaseLinesApiService } from '../purchase-lines-api.service';
import { PurchaseLinesStore } from '../purchase-lines.store';
import { PurchaseLineItemsComponent } from './purchase-line-items.component';

/**
 * Purchase line-items section (REQ-PUR-004, AC-PUR-011/012): renders the lines, shows
 * the server-derived invoice total (never a client-side sum — BR-PUR-006/DEC-PUR-003),
 * and offers add/remove only for a draft (AC-PUR-012).
 */
describe('PurchaseLineItemsComponent', () => {
  let http: HttpTestingController;

  const line = {
    id: 'l1',
    productId: 'p1',
    productName: 'أموكسيسيلين',
    purchaseUnitId: 'u1',
    purchaseUnitName: 'كرتونة',
    quantity: 3,
    unitPrice: { amount: 100, currency: 'EGP' },
    lineTotal: { amount: 300, currency: 'EGP' },
    requiresExpiry: false,
  };

  function setup(isDraft: boolean, total = { amount: 300, currency: 'EGP' }, lines: unknown[] = [line]) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), PurchaseLinesApiService, PurchaseLinesStore],
    });

    TestBed.inject(PurchaseLinesStore).setId('inv-1');
    http = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(PurchaseLineItemsComponent);
    fixture.componentRef.setInput('isDraft', isDraft);
    fixture.componentRef.setInput('total', total);

    TestBed.tick();
    http.expectOne('/api/v1/purchase-invoices/inv-1/lines').flush(lines);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    http.verify();
  });

  it('renders the lines and the server-provided invoice total', () => {
    const element: HTMLElement = setup(true).nativeElement;

    expect(element.textContent).toContain('أموكسيسيلين');
    expect(element.textContent).toContain('كرتونة');
    // The total row shows the header total from the server input (300), not a client sum.
    expect(element.querySelector('.total-value')?.textContent).toContain('300');
  });

  it('offers add and remove for a draft (AC-PUR-012)', () => {
    const element: HTMLElement = setup(true).nativeElement;

    expect(element.querySelector('.card-head vf-button')).not.toBeNull();
    expect(element.querySelector('.action-col vf-button')).not.toBeNull();
  });

  it('hides add and remove for a non-draft invoice (AC-PUR-012)', () => {
    const element: HTMLElement = setup(false).nativeElement;

    expect(element.querySelector('.card-head vf-button')).toBeNull();
    expect(element.querySelector('.action-col vf-button')).toBeNull();
  });

  it('shows the empty state and the zero total when a draft has no lines', () => {
    const element: HTMLElement = setup(true, { amount: 0, currency: 'EGP' }, []).nativeElement;

    expect(element.querySelector('vf-empty-state')).not.toBeNull();
    expect(element.querySelector('.total-value')?.textContent).toContain('0');
  });
});
