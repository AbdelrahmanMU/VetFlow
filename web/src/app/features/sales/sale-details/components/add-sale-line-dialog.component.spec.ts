import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FormControl, FormGroup } from '@angular/forms';

import { AddSaleLinePayload, ProductPickerOption } from '../sale-lines.models';
import { SaleLinesApiService } from '../sale-lines-api.service';
import { AddSaleLineDialogComponent } from './add-sale-line-dialog.component';

/**
 * Add-sale-line dialog (REQ-SAL-001) — the one place this slice deliberately diverges from the
 * purchasing mirror, so it is covered directly rather than through the store:
 * - the default sale unit is auto-selected on product choice (BR-CAT-022);
 * - the catalog price is **displayed and never entered**, and the payload carries no price
 *   (DEC-SAL-003);
 * - a **non-splittable** product rejects a fractional quantity with its own field message and
 *   emits nothing — no rounding, no truncation, no silent correction (DEC-SAL-007, BR-CAT-032),
 *   through the shared `wholeNumber` validator on the validation foundation (STD-UX-022/125).
 */
interface DialogInternals {
  onSave(): void;
  form: FormGroup<{
    productId: FormControl<string | null>;
    unitId: FormControl<string | null>;
    quantity: FormControl<number | null>;
  }>;
  selectedPrice: () => { amount: number; currency: string } | null;
  lineTotalPreview: () => number | null;
  unitOptions(): { value: string }[];
}

describe('AddSaleLineDialogComponent', () => {
  let http: HttpTestingController;

  const products: ProductPickerOption[] = [{ id: 'p1', name: 'أموكسيسيلين' }];

  const profile = (isSplittable: boolean) => ({
    isSplittable,
    units: [
      { unitId: 'u-box', unitName: 'علبة', isSaleUnit: true, isDefaultSaleUnit: false, sellingPrice: { amount: 90, currency: 'EGP' } },
      { unitId: 'u-strip', unitName: 'شريط', isSaleUnit: true, isDefaultSaleUnit: true, sellingPrice: { amount: 25, currency: 'EGP' } },
      { unitId: 'u-carton', unitName: 'كرتونة', isSaleUnit: false, isDefaultSaleUnit: false, sellingPrice: { amount: 900, currency: 'EGP' } },
    ],
  });

  function setup(isSplittable: boolean) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SaleLinesApiService],
    });

    const fixture = TestBed.createComponent(AddSaleLineDialogComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('products', products);
    fixture.componentRef.setInput('visible', true);
    TestBed.tick();
    fixture.detectChanges();

    const internals = fixture.componentInstance as unknown as DialogInternals;
    internals.form.controls.productId.setValue('p1');
    http.expectOne('/api/v1/products/p1').flush(profile(isSplittable));
    TestBed.tick();

    return { fixture, internals };
  }

  afterEach(() => {
    http.verify();
  });

  it('auto-selects the default sale unit and shows its catalog price read-only (BR-CAT-022, DEC-SAL-003)', () => {
    const { fixture, internals } = setup(true);

    expect(internals.form.controls.unitId.value).toBe('u-strip');
    expect(internals.selectedPrice()).toEqual({ amount: 25, currency: 'EGP' });
    // Only the quantity is a numeric input; the price is rendered, never entered.
    const priceInputs = [...fixture.nativeElement.querySelectorAll('vf-number-input')];
    expect(priceInputs.length).toBe(1);
  });

  it('offers sale units only — a purchase-only unit is never selectable (BR-SAL-004)', () => {
    const { internals } = setup(true);

    expect(internals.unitOptions().map((option) => option.value)).toEqual(['u-box', 'u-strip']);
  });

  it('rejects a fractional quantity for a non-splittable product with its own sentence (DEC-SAL-007, STD-UX-017)', () => {
    const { fixture, internals } = setup(false);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.form.controls.quantity.setValue(1.5);
    internals.onSave();
    fixture.detectChanges();

    expect(emitted).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'هذا المنتج غير قابل للتجزئة — أدخِل كمية صحيحة.',
    );
  });

  it('accepts a whole quantity for the same non-splittable product', () => {
    const { fixture, internals } = setup(false);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.form.controls.quantity.setValue(2);
    internals.onSave();

    // No price field: the server snapshots the catalog price (BR-SAL-006, DEC-SAL-003).
    expect(emitted).toEqual([{ productId: 'p1', saleUnitId: 'u-strip', quantity: 2 }]);
  });

  it('accepts a fractional quantity when the product is splittable (BR-CAT-032)', () => {
    const { fixture, internals } = setup(true);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.form.controls.quantity.setValue(2.5);
    internals.onSave();
    fixture.detectChanges();

    expect(emitted).toEqual([{ productId: 'p1', saleUnitId: 'u-strip', quantity: 2.5 }]);
    expect((fixture.nativeElement as HTMLElement).querySelector('.vf-msg-error')).toBeNull();
  });

  it('rejects a zero or negative quantity before splittability is even considered', () => {
    const { fixture, internals } = setup(true);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.form.controls.quantity.setValue(0);
    internals.onSave();
    fixture.detectChanges();

    expect(emitted).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('الكمية يجب أن تكون أكبر من صفر.');
  });

  it('previews the line total from the catalog price, rounded to two decimals (BR-SAL-007)', () => {
    const { internals } = setup(true);

    internals.form.controls.quantity.setValue(3);
    expect(internals.lineTotalPreview()).toBe(75);

    internals.form.controls.quantity.setValue(0.333);
    expect(internals.lineTotalPreview()).toBe(8.33);
  });

  it('clears the unit and price when the product selection is cleared', () => {
    const { internals } = setup(true);

    internals.form.controls.productId.setValue(null);

    expect(internals.form.controls.unitId.value).toBeNull();
    expect(internals.selectedPrice()).toBeNull();
  });
});
