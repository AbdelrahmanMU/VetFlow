import { TestBed } from '@angular/core/testing';

import { PurchaseStatus } from '../purchase-list.models';
import { PurchaseStatusBadgeComponent } from './purchase-status-badge.component';

describe('PurchaseStatusBadgeComponent', () => {
  async function render(status: PurchaseStatus): Promise<HTMLElement> {
    await TestBed.configureTestingModule({ imports: [PurchaseStatusBadgeComponent] }).compileComponents();
    const fixture = TestBed.createComponent(PurchaseStatusBadgeComponent);
    fixture.componentRef.setInput('status', status);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('a draft invoice shows the draft badge (AC-PUR-002)', async () => {
    const element = await render('draft');
    expect(element.textContent).toContain('مسودة');
  });

  it('a received invoice shows the received badge (AC-PUR-002)', async () => {
    const element = await render('received');
    expect(element.textContent).toContain('مستلمة');
  });

  it('a cancelled invoice shows the cancelled badge (AC-PUR-002)', async () => {
    const element = await render('cancelled');
    expect(element.textContent).toContain('ملغاة');
  });
});
