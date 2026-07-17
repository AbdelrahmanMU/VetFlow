import { TestBed } from '@angular/core/testing';

import { PurchaseListItem, PurchaseSort } from '../purchase-list.models';
import { PurchaseTableComponent } from './purchase-table.component';

describe('PurchaseTableComponent', () => {
  const invoice: PurchaseListItem = {
    id: 'pi-7',
    number: 'PUR-000007',
    supplierName: 'مورد الإمداد',
    supplierInvoiceReference: null,
    invoiceDate: '2026-05-20',
    status: 'draft',
    total: { amount: 100, currency: 'EGP' },
    createdAt: '2026-05-20T09:00:00Z',
  };

  const sort: PurchaseSort = { field: 'invoiceDate', direction: 'desc' };

  it('emits open with the invoice id when a row is activated (REQ-PUR-002)', async () => {
    await TestBed.configureTestingModule({ imports: [PurchaseTableComponent] }).compileComponents();
    const fixture = TestBed.createComponent(PurchaseTableComponent);
    fixture.componentRef.setInput('rows', [invoice]);
    fixture.componentRef.setInput('sort', sort);

    let opened: string | null = null;
    fixture.componentInstance.open.subscribe((id) => (opened = id));

    await fixture.whenStable();
    const row = (fixture.nativeElement as HTMLElement).querySelector<HTMLTableRowElement>('tr.clickable-row');
    row?.click();

    expect(opened).toBe('pi-7');
  });
});
