import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { PurchaseCreatePageComponent } from './purchase-create-page.component';

/**
 * Create-purchase page (REQ-PUR-003, AC-PUR-006/007), on the validation
 * foundation (validation-and-guidance.md): submit runs through the shared
 * guidance — an invalid form neither POSTs nor navigates and focuses its first
 * invalid field (STD-UX-012/070); the date field blur-validates through the
 * CVA (moment 2); a valid form POSTs the header then navigates to the new
 * invoice's Details; omitted optionals serialize as null (not `""`); failures
 * classify — server field errors project inline (STD-UX-019), others render a
 * banner that clears on the next edit (STD-UX-035).
 */
describe('PurchaseCreatePageComponent', () => {
  let http: HttpTestingController;

  function setup(): ComponentFixture<PurchaseCreatePageComponent> {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(PurchaseCreatePageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return fixture;
  }

  function fillValidForm(component: PurchaseCreatePageComponent): void {
    component.form.controls.supplierName.setValue('مورد الأدوية البيطرية');
    component.form.controls.invoiceDate.setValue('2026-07-17');
  }

  function submitForm(fixture: ComponentFixture<PurchaseCreatePageComponent>): void {
    const form = (fixture.nativeElement as HTMLElement).querySelector('form');
    if (!form) {
      throw new Error('form not rendered');
    }

    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();
  }

  afterEach(() => {
    http.verify();
  });

  it('blocks submit, shows both required errors, and focuses the first invalid field (AC-PUR-007, STD-UX-012/070)', async () => {
    const fixture = setup();

    submitForm(fixture);

    // Nothing is written when the form is invalid.
    http.expectNone((request) => request.method === 'POST');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text.split('هذا الحقل مطلوب.').length - 1).toBe(2);

    // The shared guidance focuses the first invalid control one tick later.
    await new Promise((resolve) => setTimeout(resolve, 0));
    const firstInvalid = (fixture.nativeElement as HTMLElement).querySelector(
      '.vf-field--invalid input',
    );
    expect(document.activeElement).toBe(firstInvalid);
  });

  it('the date field shows its error on blur — moment 2 through the CVA', () => {
    const fixture = setup();

    const dateInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[type="date"]',
    );
    if (!dateInput) {
      throw new Error('date input not rendered');
    }

    dateInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('هذا الحقل مطلوب.');
  });

  it('POSTs the header and navigates to the new invoice details on success (AC-PUR-006)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
    submitForm(fixture);

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
    submitForm(fixture);

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
    submitForm(fixture);

    const create = http.expectOne((request) => request.method === 'POST');
    expect(create.request.body.supplierName).toBe('مورد');
    expect(create.request.body.supplierInvoiceReference).toBe('REF-42');
    expect(create.request.body.notes).toBe('ملاحظة');
    create.flush({ id: 'inv-1', number: 'PUR-000001' });
  });

  it('a code-less failure renders the classified banner and clears on the next edit (STD-UX-035)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fillValidForm(component);
    submitForm(fixture);

    const create = http.expectOne((request) => request.method === 'POST');
    create.flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
    expect(banner?.textContent).toContain('تعذّر إنشاء فاتورة الشراء');
    expect(banner?.getAttribute('role')).toBe('alert');
    expect(component.saving()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();

    // The banner never survives the edit that addresses it.
    component.form.controls.supplierName.setValue('مورد آخر');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
  });

  it('a VTF-VAL-001 field error projects inline onto its field — not a banner (STD-UX-019/020)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // A whitespace-only name passes the client `required` but trims to empty
    // in the payload — the one real client-side escape to VTF-VAL-001.
    component.form.controls.supplierName.setValue('   ');
    component.form.controls.invoiceDate.setValue('2026-07-17');
    submitForm(fixture);

    const create = http.expectOne((request) => request.method === 'POST');
    create.flush(
      {
        type: 'about:blank',
        title: 'Bad Request',
        status: 400,
        errorCode: 'VTF-VAL-001',
        errors: { supplierName: ['server text (never rendered)'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('راجع قيمة هذا الحقل.');
  });
});
