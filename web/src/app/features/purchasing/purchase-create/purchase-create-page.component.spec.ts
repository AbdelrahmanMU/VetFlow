import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { PurchaseCreatePageComponent } from './purchase-create-page.component';

/**
 * Create-purchase page (REQ-PUR-003, AC-PUR-006/007). Mirrors the product-editor
 * create spec: an invalid form neither POSTs nor navigates; a valid form POSTs the
 * header then navigates to the new invoice's Details; omitted optionals serialize
 * as null (not `""`), so Details keeps rendering «—» for a missing reference.
 */
describe('PurchaseCreatePageComponent', () => {
  let http: HttpTestingController;

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(PurchaseCreatePageComponent);
    http = TestBed.inject(HttpTestingController);
    return fixture;
  }

  function fillValidForm(component: PurchaseCreatePageComponent): void {
    component.form.controls.supplierName.setValue('مورد الأدوية البيطرية');
    component.form.controls.invoiceDate.setValue('2026-07-17');
  }

  afterEach(() => {
    http.verify();
  });

  it('blocks submit and surfaces errors when the required fields are missing (AC-PUR-007)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;

    component.submit();

    expect(component.submitted()).toBe(true);
    expect(component.form.controls.supplierName.invalid).toBe(true);
    expect(component.form.controls.invoiceDate.invalid).toBe(true);
    // Nothing is written when the form is invalid.
    http.expectNone((request) => request.method === 'POST');
  });

  it('POSTs the header and navigates to the new invoice details on success (AC-PUR-006)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
    expect(component.form.valid).toBe(true);
    component.submit();

    const create = http.expectOne(
      (request) => request.url === '/api/v1/purchase-invoices' && request.method === 'POST',
    );
    expect(create.request.body.supplierName).toBe('مورد الأدوية البيطرية');
    expect(create.request.body.invoiceDate).toBe('2026-07-17');
    create.flush({ id: 'inv-1', number: 'PUR-000001' });

    expect(navigate).toHaveBeenCalledWith(['/purchases', 'inv-1']);
  });

  it('serializes omitted optional fields as null, never an empty string (TS-PUR-014)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
    component.submit();

    const create = http.expectOne(
      (request) => request.url === '/api/v1/purchase-invoices' && request.method === 'POST',
    );
    expect(create.request.body.supplierInvoiceReference).toBeNull();
    expect(create.request.body.notes).toBeNull();
    create.flush({ id: 'inv-1', number: 'PUR-000001' });
  });

  it('trims the supplier name and passes optional values through when present', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.supplierName.setValue('  مورد  ');
    component.form.controls.supplierInvoiceReference.setValue('REF-42');
    component.form.controls.invoiceDate.setValue('2026-07-17');
    component.form.controls.notes.setValue('ملاحظة');
    component.submit();

    const create = http.expectOne((request) => request.method === 'POST');
    expect(create.request.body.supplierName).toBe('مورد');
    expect(create.request.body.supplierInvoiceReference).toBe('REF-42');
    expect(create.request.body.notes).toBe('ملاحظة');
    create.flush({ id: 'inv-1', number: 'PUR-000001' });
  });

  it('shows an error banner and does not navigate when the create fails', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
    component.submit();

    const create = http.expectOne((request) => request.method === 'POST');
    create.flush(
      { type: 'about:blank', title: 'Bad Request', status: 400 },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.banner()).toBe(true);
    expect(component.saving()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });
});
