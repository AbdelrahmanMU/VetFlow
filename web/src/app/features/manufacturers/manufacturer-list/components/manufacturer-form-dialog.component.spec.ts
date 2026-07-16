import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManufacturerFormDialogComponent } from './manufacturer-form-dialog.component';

describe('ManufacturerFormDialogComponent', () => {
  async function open(
    mode: 'create' | 'rename',
    initialName = '',
  ): Promise<{ fixture: ComponentFixture<ManufacturerFormDialogComponent>; saved: string[] }> {
    await TestBed.configureTestingModule({ imports: [ManufacturerFormDialogComponent] }).compileComponents();
    const fixture = TestBed.createComponent(ManufacturerFormDialogComponent);
    const saved: string[] = [];
    fixture.componentInstance.save.subscribe((value) => saved.push(value));
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('initialName', initialName);
    fixture.componentRef.setInput('visible', true);
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, saved };
  }

  function type(fixture: ComponentFixture<ManufacturerFormDialogComponent>, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector('input');
    if (!input) {
      throw new Error('name input not rendered');
    }

    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function clickSave(fixture: ComponentFixture<ManufacturerFormDialogComponent>): void {
    const saveButton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '.vf-button--primary',
    );
    saveButton?.click();
    fixture.detectChanges();
  }

  it('an empty name blocks save and shows the required error', async () => {
    const { fixture, saved } = await open('create');

    clickSave(fixture);

    expect(saved).toEqual([]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('اسم الشركة المصنعة مطلوب');
  });

  it('a valid name is emitted trimmed', async () => {
    const { fixture, saved } = await open('create');

    type(fixture, '  شركة الأمل  ');
    clickSave(fixture);

    expect(saved).toEqual(['شركة الأمل']);
  });

  it('rename mode pre-fills the current name and uses the rename title', async () => {
    const { fixture } = await open('rename', 'شركة الأمل');

    const input = (fixture.nativeElement as HTMLElement).querySelector('input');
    expect(input?.value).toBe('شركة الأمل');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('إعادة تسمية الشركة المصنعة');
  });

  it('a server field error is displayed and clears once the user edits', async () => {
    const { fixture } = await open('create');

    fixture.componentRef.setInput('serverError', 'توجد شركة مصنعة بهذا الاسم بالفعل.');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('توجد شركة مصنعة بهذا الاسم بالفعل');

    type(fixture, 'شركة جديدة');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('توجد شركة مصنعة بهذا الاسم بالفعل');
  });
});
