import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { SaleCreatePageComponent } from './sale-create-page.component';

/**
 * Create-sale page (REQ-SAL-001, AC-SAL-001/002). Mirrors the create-purchase spec: an invalid
 * form neither POSTs nor navigates; a valid form POSTs the header then navigates to the new
 * invoice's Details; the customer name and notes are optional (DEC-SAL-002) and serialize as
 * null — never `""` — so Details keeps rendering «—» for a missing value.
 */
describe('SaleCreatePageComponent', () => {
  let http: HttpTestingController;

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(SaleCreatePageComponent);
    http = TestBed.inject(HttpTestingController);
    return fixture;
  }

  afterEach(() => {
    http.verify();
  });

  it('blocks submit and surfaces the error when the sale date is missing (AC-SAL-002)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;

    component.submit();

    expect(component.submitted()).toBe(true);
    expect(component.form.controls.saleDate.invalid).toBe(true);
    // Nothing is written when the form is invalid.
    http.expectNone((request) => request.method === 'POST');
  });

  it('creates with the sale date alone — customer and notes are optional (AC-SAL-002, DEC-SAL-002)', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    expect(component.form.valid).toBe(true);
    component.submit();

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
    component.submit();

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
    component.submit();

    const create = http.expectOne((request) => request.method === 'POST');
    expect(create.request.body.customerName).toBe('عيادة النور');
    expect(create.request.body.notes).toBe('ملاحظة');
    create.flush({ id: 'si-1', number: 'SAL-000001' });
  });

  it('shows an error banner and does not navigate when the create fails', () => {
    const fixture = setup();
    const component = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    component.form.controls.saleDate.setValue('2026-07-30');
    component.submit();

    http.expectOne((request) => request.method === 'POST').flush(
      { type: 'about:blank', title: 'Bad Request', status: 400 },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.banner()).toBe(true);
    expect(component.saving()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });
});
