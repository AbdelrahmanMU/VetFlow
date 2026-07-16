import { TestBed } from '@angular/core/testing';

import { CategoryStatusBadgeComponent } from './category-status-badge.component';

describe('CategoryStatusBadgeComponent', () => {
  async function render(isActive: boolean): Promise<HTMLElement> {
    await TestBed.configureTestingModule({ imports: [CategoryStatusBadgeComponent] }).compileComponents();
    const fixture = TestBed.createComponent(CategoryStatusBadgeComponent);
    fixture.componentRef.setInput('isActive', isActive);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('an active category shows the active badge', async () => {
    const element = await render(true);
    expect(element.textContent).toContain('نشط');
    expect(element.textContent).not.toContain('غير نشط');
  });

  it('an inactive category shows the inactive badge', async () => {
    const element = await render(false);
    expect(element.textContent).toContain('غير نشط');
  });
});
