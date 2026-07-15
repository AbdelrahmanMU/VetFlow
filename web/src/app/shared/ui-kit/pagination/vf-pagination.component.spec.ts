import { TestBed } from '@angular/core/testing';

import { VfPaginationComponent } from './vf-pagination.component';

describe('VfPaginationComponent', () => {
  async function create(page: number, pageSize: number, totalCount: number) {
    await TestBed.configureTestingModule({ imports: [VfPaginationComponent] }).compileComponents();
    const fixture = TestBed.createComponent(VfPaginationComponent);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('totalCount', totalCount);
    await fixture.whenStable();
    return fixture;
  }

  it('shows a clear result counter with western digits', async () => {
    const fixture = await create(2, 25, 137);
    const counter = (fixture.nativeElement as HTMLElement).querySelector('.counter');
    expect(counter?.textContent).toContain('26');
    expect(counter?.textContent).toContain('50');
    expect(counter?.textContent).toContain('137');
  });

  it('disables the previous button on the first page', async () => {
    const fixture = await create(1, 25, 60);
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.page-button');
    expect(buttons[0].disabled).toBe(true);
    expect(buttons[1].disabled).toBe(false);
  });

  it('disables the next button on the last page', async () => {
    const fixture = await create(3, 25, 60);
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.page-button');
    expect(buttons[0].disabled).toBe(false);
    expect(buttons[1].disabled).toBe(true);
  });

  it('emits the requested page', async () => {
    const fixture = await create(2, 25, 137);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));
    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.page-button');
    buttons[0].click();
    buttons[1].click();
    expect(emitted).toEqual([1, 3]);
  });
});
