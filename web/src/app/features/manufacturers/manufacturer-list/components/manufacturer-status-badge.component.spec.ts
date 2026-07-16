import { TestBed } from '@angular/core/testing';

import { ManufacturerStatusBadgeComponent } from './manufacturer-status-badge.component';

describe('ManufacturerStatusBadgeComponent', () => {
  async function render(isActive: boolean): Promise<HTMLElement> {
    await TestBed.configureTestingModule({ imports: [ManufacturerStatusBadgeComponent] }).compileComponents();
    const fixture = TestBed.createComponent(ManufacturerStatusBadgeComponent);
    fixture.componentRef.setInput('isActive', isActive);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('an active manufacturer shows the active badge', async () => {
    const element = await render(true);
    expect(element.textContent).toContain('نشط');
    expect(element.textContent).not.toContain('غير نشط');
  });

  it('an inactive manufacturer shows the inactive badge', async () => {
    const element = await render(false);
    expect(element.textContent).toContain('غير نشط');
  });
});
