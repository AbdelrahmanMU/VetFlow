import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { SaleCreatePageComponent } from './sale-create-page.component';

/**
 * Create-sale page (REQ-SAL-001, AC-SAL-001/002), on the validation
 * foundation — mirror of the create-purchase spec: submit runs through the
 * shared guidance; the sale date blur-validates through the CVA (moment 2);
 * optionals (DEC-SAL-002) serialize as null; failures classify — server
 * field errors project inline (STD-UX-019), others render the shared banner
 * that clears on the next edit (STD-UX-035).
 */
describe('SaleCreatePageComponent', () => {
  let http: HttpTestingController;

  function setup(): ComponentFixture<SaleCreatePageComponent> {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(SaleCreatePageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return fixture;
  }

  function submitForm(fixture: ComponentFixture<SaleCreatePageComponent>): void {
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

  it('blocks submit, shows the date error, and focuses the invalid field (AC-SAL-002, STD-UX-012/070)', async () => {
    const fixture = setup();

    submitForm(fixture);

    http.expectNone((request) => request.method === 'POST');
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    // The date is the only required field (DEC-SAL-002) — exactly one error.
    expect(text.split('هذا الحقل مطلوب.').length - 1).toBe(1);

    await new Promise((resolve) => setTimeout(resolve, 0));
    const firstInvalid = (fixture.nativeElement as HTMLElement).querySelector('.vf-field--invalid input');
    expect(document.activeElement).toBe(firstInvalid);
  });

  it('the date field shows its error on blur — moment 2 through the CVA', () => {
    const fixture = setup();

    const dateInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="date"]');
    if (!dateInput) {
      throw new Error('date input not rendered');
    }

    dateInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('هذا الحقل مطلوب.');
  });

  it('creates with the sale date alone — customer and notes are optional and null (AC-SAL-002, DEC-SAL-002)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    submitForm(fixture);

    const create = http.expectOne((request) => request.url === '/api/v1/sales-invoices' && request.method === 'POST');
    expect(create.request.body.saleDate).toBe('2026-07-30');
    expect(create.request.body.customerName).toBeNull();
    expect(create.request.body.notes).toBeNull();
    create.flush({ id: 'si-1', number: 'SAL-000001' });
  });

  it('POSTs the header and navigates to the new invoice details on success (AC-SAL-001)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    submitForm(fixture);

    http.expectOne((request) => request.method === 'POST').flush({ id: 'si-1', number: 'SAL-000001' });

    expect(navigate).toHaveBeenCalledWith(['/sales', 'si-1']);
  });

  it('trims the optional values it does send', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.customerName.setValue('  عيادة النور  ');
    component.form.controls.saleDate.setValue('2026-07-30');
    component.form.controls.notes.setValue('  ملاحظة  ');
    submitForm(fixture);

    const create = http.expectOne((request) => request.method === 'POST');
    expect(create.request.body.customerName).toBe('عيادة النور');
    expect(create.request.body.notes).toBe('ملاحظة');
    create.flush({ id: 'si-1', number: 'SAL-000001' });
  });

  it('a code-less failure renders the classified banner and clears on the next edit (STD-UX-035)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    submitForm(fixture);

    http.expectOne((request) => request.method === 'POST').flush(
      { type: 'about:blank', title: 'Internal Server Error', status: 500 },
      { status: 500, statusText: 'Internal Server Error' },
    );
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
    expect(banner?.textContent).toContain('تعذّر إنشاء فاتورة البيع');
    expect(banner?.getAttribute('role')).toBe('alert');
    expect(component.saving()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();

    component.form.controls.customerName.setValue('عميل');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
  });

  it('a VTF-VAL-001 field error projects inline onto its field — not a banner (STD-UX-019/020)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    submitForm(fixture);

    http.expectOne((request) => request.method === 'POST').flush(
      {
        type: 'about:blank',
        title: 'Bad Request',
        status: 400,
        errorCode: 'VTF-VAL-001',
        errors: { saleDate: ['server text (never rendered)'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('راجع قيمة هذا الحقل.');
  });
});
