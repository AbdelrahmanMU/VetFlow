import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { SalesReturnPageComponent } from './sales-return-page.component';

/**
 * The commit-confirmation gate (sales ui.md §«مرتجع مبيعات جديد»): committing a return
 * is irreversible and moves stock, so pressing «تثبيت المرتجع» must open a confirmation
 * and write nothing until it is confirmed. Before this gate existed the screen committed
 * on the first press — the defect these tests pin. Mirrors the purchase-return spec.
 */
describe('SalesReturnPageComponent — commit confirmation', () => {
  let fixture: ComponentFixture<SalesReturnPageComponent>;
  let http: HttpTestingController;

  const RETURNABLE_URL = '/api/v1/sales-invoices/invoice-1/returnable-lines';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SalesReturnPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'invoice-1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SalesReturnPageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    http.expectOne((candidate) => candidate.url === RETURNABLE_URL).flush([
      {
        salesLineItemId: 'line-a',
        productId: 'product-a',
        productName: 'أموكسيسيللين ٥٠٠ ملغم',
        saleUnitName: 'علبة',
        quantity: 3,
        returnedQuantity: 0,
        returnableQuantity: 3,
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function enterQuantity(value: number): void {
    const input = element().querySelector<HTMLInputElement>('table input');
    if (!input) {
      throw new Error('quantity input not rendered');
    }

    input.value = String(value);
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submitForm(): void {
    const form = element().querySelector('form');
    form?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function dialogConfirmButton(): HTMLButtonElement | null {
    return document.querySelector<HTMLButtonElement>('.vf-dialog .vf-button--primary');
  }

  it('submitting opens the confirmation and writes nothing yet', () => {
    enterQuantity(1);
    submitForm();

    expect(document.body.textContent).toContain('تثبيت المرتجع نهائيّ ولا يمكن التراجع عنه');
    expect(dialogConfirmButton()).not.toBeNull();
  });

  it('confirming performs the create → add line → commit sequence', () => {
    enterQuantity(1);
    submitForm();
    dialogConfirmButton()?.click();
    fixture.detectChanges();

    http
      .expectOne((candidate) => candidate.url === '/api/v1/sales-returns')
      .flush({ id: 'ret-1', number: 'SRT-000001' });

    const line = http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/lines');
    expect(line.request.body).toEqual({ salesLineItemId: 'line-a', quantity: 1 });
    line.flush({ id: 'rl-1' });

    http.expectOne((candidate) => candidate.url === '/api/v1/sales-returns/ret-1/commit').flush(null);
    fixture.detectChanges();

    expect(element().textContent).toContain('SRT-000001');
  });

  it('cancelling the confirmation leaves the form intact and writes nothing', () => {
    enterQuantity(1);
    submitForm();

    document.querySelector<HTMLButtonElement>('.vf-dialog .vf-button--quiet')?.click();
    fixture.detectChanges();

    expect(element().querySelector<HTMLInputElement>('table input')?.value).toBe('1');
  });

  it('an invalid form reports its fields instead of opening the confirmation', () => {
    const date = element().querySelector<HTMLInputElement>('.date-field input');
    if (!date) {
      throw new Error('return-date input not rendered');
    }

    date.value = '';
    date.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    submitForm();

    expect(dialogConfirmButton()).toBeNull();
    expect(element().textContent).toContain('هذا الحقل مطلوب');
  });
});
