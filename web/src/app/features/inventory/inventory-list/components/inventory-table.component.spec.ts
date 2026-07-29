import { TestBed } from '@angular/core/testing';

import { InventoryItem, InventorySort } from '../inventory-list.models';
import { InventoryTableComponent } from './inventory-table.component';

describe('InventoryTableComponent', () => {
  const item: InventoryItem = {
    productId: 'prod-7',
    productName: 'باراسيتامول',
    onHandQuantity: 24,
    stockUnitName: 'شريط',
    batchCount: 2,
    nearestExpiry: '2026-08-20',
  };

  const sort: InventorySort = { field: 'product', direction: 'asc' };

  async function render(row: InventoryItem): Promise<HTMLElement> {
    await TestBed.configureTestingModule({ imports: [InventoryTableComponent] }).compileComponents();
    const fixture = TestBed.createComponent(InventoryTableComponent);
    fixture.componentRef.setInput('rows', [row]);
    fixture.componentRef.setInput('sort', sort);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('emits open with the product id when a row is activated (BR-INV-015)', async () => {
    await TestBed.configureTestingModule({ imports: [InventoryTableComponent] }).compileComponents();
    const fixture = TestBed.createComponent(InventoryTableComponent);
    fixture.componentRef.setInput('rows', [item]);
    fixture.componentRef.setInput('sort', sort);

    let opened: string | null = null;
    fixture.componentInstance.open.subscribe((id) => (opened = id));

    await fixture.whenStable();
    const row = (fixture.nativeElement as HTMLElement).querySelector<HTMLTableRowElement>('tr.clickable-row');
    row?.click();

    expect(opened).toBe('prod-7');
  });

  it('renders the placeholder dash when a product has no nearest expiry (BR-INV-010)', async () => {
    const element = await render({ ...item, nearestExpiry: null });
    expect(element.textContent).toContain('—');
  });
});
