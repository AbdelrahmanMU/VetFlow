import { TestBed } from '@angular/core/testing';

import { ProductListItem } from '../product-list.models';
import { ProductStatusBadgesComponent } from './product-status-badges.component';

function product(overrides: Partial<ProductListItem>): ProductListItem {
  return {
    id: '1',
    arabicName: 'منتج',
    englishName: null,
    size: null,
    concentration: null,
    categoryName: 'تصنيف',
    manufacturerName: 'مصنع',
    natureName: 'دواء',
    storageUnitName: 'قرص',
    defaultSaleUnitName: 'علبة',
    defaultSaleUnitPrice: null,
    status: 'active',
    isSplittable: false,
    isRefrigerated: false,
    hasExpiration: false,
    hasSellingPrice: true,
    ...overrides,
  };
}

describe('ProductStatusBadgesComponent', () => {
  async function render(item: ProductListItem): Promise<HTMLElement> {
    await TestBed.configureTestingModule({ imports: [ProductStatusBadgesComponent] }).compileComponents();
    const fixture = TestBed.createComponent(ProductStatusBadgesComponent);
    fixture.componentRef.setInput('product', item);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('an active priced product shows the active badge only', async () => {
    const element = await render(product({}));
    expect(element.textContent).toContain('نشط');
    expect(element.textContent).not.toContain('بلا سعر');
  });

  it('a product without any selling price carries the unsellable badge (BR-CAT-026)', async () => {
    const element = await render(product({ hasSellingPrice: false }));
    expect(element.textContent).toContain('بلا سعر');
  });

  it('a disabled product shows the disabled badge', async () => {
    const element = await render(product({ status: 'disabled' }));
    expect(element.textContent).toContain('معطَّل');
  });
});
