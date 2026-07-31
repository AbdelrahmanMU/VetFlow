import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { MovementHistoryItem } from '../movement-history.models';
import { MovementHistoryTableComponent } from './movement-history-table.component';

describe('MovementHistoryTableComponent', () => {
  const receive: MovementHistoryItem = {
    movementId: 'mv-1',
    occurredAt: '2026-07-31T10:00:00+03:00',
    type: 'receive',
    productName: 'باراسيتامول',
    batchId: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
    quantity: 24,
    stockUnitName: 'شريط',
    referenceLabel: 'PUR-000001',
    referenceTarget: 'purchaseInvoice',
    referenceId: 'inv-9',
    source: 'purchasing',
  };

  async function render(...rows: MovementHistoryItem[]): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [MovementHistoryTableComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    const fixture = TestBed.createComponent(MovementHistoryTableComponent);
    fixture.componentRef.setInput('rows', rows);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders exactly the seven frozen columns and no action column (AC-INV-031)', async () => {
    const element = await render(receive);

    const headers = [...element.querySelectorAll('th')].map((th) => th.textContent?.trim());
    expect(headers).toEqual([
      'التاريخ',
      'نوع الحركة',
      'المنتج',
      'الدفعة',
      'الكمية',
      'المرجع',
      'الوحدة المصدر',
    ]);
    expect(element.querySelector('button')).toBeNull();
  });

  it('links a receive to its purchase invoice (AC-INV-033)', async () => {
    const element = await render(receive);

    const link = element.querySelector<HTMLAnchorElement>('a.reference-link');
    expect(link?.textContent?.trim()).toBe('PUR-000001');
    expect(link?.getAttribute('href')).toBe('/purchases/inv-9');
  });

  it('links a consumption to its sales invoice and shows the decrease signed (AC-INV-033, BR-INV-064)', async () => {
    const element = await render({
      ...receive,
      movementId: 'mv-2',
      type: 'consume',
      quantity: -3,
      referenceLabel: 'SAL-000004',
      referenceTarget: 'salesInvoice',
      referenceId: 'sale-4',
      source: 'sales',
    });

    const link = element.querySelector<HTMLAnchorElement>('a.reference-link');
    expect(link?.getAttribute('href')).toBe('/sales/sale-4');
    // The decrease keeps the sign the ledger recorded and is never shown as a bare magnitude.
    // Digit shaping is the formatter's business, so only the sign and the value are asserted here.
    const outgoing = element.querySelector('.quantity--out')?.textContent ?? '';
    expect(outgoing).toContain('3');
    expect(outgoing).toContain('-');
    expect(outgoing).not.toContain('+');
    expect(element.textContent).toContain('المبيعات');
  });

  it('shows a dash and no link for an inventory-native movement (BR-INV-043)', async () => {
    const element = await render({
      ...receive,
      movementId: 'mv-3',
      type: 'writeOff',
      quantity: -2,
      referenceLabel: null,
      referenceTarget: 'none',
      referenceId: null,
      source: 'inventory',
    });

    expect(element.querySelector('a.reference-link')).toBeNull();
    expect(element.textContent).toContain('—');
    expect(element.textContent).toContain('إهلاك');
  });

  it('marks an increase as incoming so the direction is visible (BR-INV-064)', async () => {
    const element = await render(receive);
    expect(element.querySelector('.quantity--in')).not.toBeNull();
  });
});
