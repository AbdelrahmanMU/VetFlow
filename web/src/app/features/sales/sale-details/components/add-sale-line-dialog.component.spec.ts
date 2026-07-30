import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AddSaleLinePayload, ProductPickerOption } from '../sale-lines.models';
import { SaleLinesApiService } from '../sale-lines-api.service';
import { AddSaleLineDialogComponent } from './add-sale-line-dialog.component';

/**
 * Add-sale-line dialog (REQ-SAL-001) — the one place this slice deliberately diverges from the
 * purchasing mirror, so it is covered directly rather than through the store:
 * - the default sale unit is auto-selected on product choice (BR-CAT-022);
 * - the catalog price is **displayed and never entered**, and the payload carries no price
 *   (DEC-SAL-003);
 * - a **non-splittable** product rejects a fractional quantity with a field message and emits
 *   nothing — no rounding, no truncation, no silent correction (DEC-SAL-007, BR-CAT-032).
 */
interface DialogInternals {
  onProductChange(productId: string | null): void;
  onSave(): void;
  quantity: { set(value: number | null): void };
  selectedUnitId: () => string | null;
  quantityError: () => string | null;
  selectedPrice: () => { amount: number; currency: string } | null;
  lineTotalPreview: () => number | null;
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
    internals.onProductChange('p1');
    http.expectOne('/api/v1/products/p1').flush(profile(isSplittable));
    TestBed.tick();

    return { fixture, internals };
  }

  afterEach(() => {
    http.verify();
  });

  it('auto-selects the default sale unit and shows its catalog price read-only (BR-CAT-022, DEC-SAL-003)', () => {
    const { fixture, internals } = setup(true);

    expect(internals.selectedUnitId()).toBe('u-strip');
    expect(internals.selectedPrice()).toEqual({ amount: 25, currency: 'EGP' });
    // Only the picker's own filter field is an input; the price is rendered, never entered.
    const priceInputs = [...fixture.nativeElement.querySelectorAll('vf-number-input')];
    expect(priceInputs.length).toBe(1);
  });

  it('offers sale units only — a purchase-only unit is never selectable (BR-SAL-004)', () => {
    const { internals } = setup(true);
    const options = (internals as unknown as { unitOptions(): { value: string }[] }).unitOptions();

    expect(options.map((option) => option.value)).toEqual(['u-box', 'u-strip']);
  });

  it('rejects a fractional quantity for a non-splittable product and emits nothing (DEC-SAL-007)', () => {
    const { fixture, internals } = setup(false);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.quantity.set(1.5);
    internals.onSave();

    expect(emitted).toEqual([]);
    expect(internals.quantityError()).toBe('هذا المنتج غير قابل للتجزئة — أدخِل كمية صحيحة.');
  });

  it('accepts a whole quantity for the same non-splittable product', () => {
    const { fixture, internals } = setup(false);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.quantity.set(2);
    internals.onSave();

    // No price field: the server snapshots the catalog price (BR-SAL-006, DEC-SAL-003).
    expect(emitted).toEqual([{ productId: 'p1', saleUnitId: 'u-strip', quantity: 2 }]);
  });

  it('accepts a fractional quantity when the product is splittable (BR-CAT-032)', () => {
    const { fixture, internals } = setup(true);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.quantity.set(2.5);
    internals.onSave();

    expect(emitted).toEqual([{ productId: 'p1', saleUnitId: 'u-strip', quantity: 2.5 }]);
    expect(internals.quantityError()).toBeNull();
  });

  it('rejects a zero or negative quantity before splittability is even considered', () => {
    const { fixture, internals } = setup(true);
    const emitted: AddSaleLinePayload[] = [];
    fixture.componentInstance.save.subscribe((payload) => emitted.push(payload));

    internals.quantity.set(0);
    internals.onSave();

    expect(emitted).toEqual([]);
    expect(internals.quantityError()).toBe('الكمية يجب أن تكون أكبر من صفر.');
  });

  it('previews the line total from the catalog price, rounded to two decimals (BR-SAL-007)', () => {
    const { internals } = setup(true);

    internals.quantity.set(3);
    expect(internals.lineTotalPreview()).toBe(75);

    internals.quantity.set(0.333);
    expect(internals.lineTotalPreview()).toBe(8.33);
  });

  it('clears the unit and price when the product selection is cleared', () => {
    const { internals } = setup(true);

    internals.onProductChange(null);

    expect(internals.selectedUnitId()).toBeNull();
    expect(internals.selectedPrice()).toBeNull();
  });
});
