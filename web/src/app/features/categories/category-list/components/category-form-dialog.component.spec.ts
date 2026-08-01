import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { CategoryFormDialogComponent } from './category-form-dialog.component';

describe('CategoryFormDialogComponent', () => {
  async function open(
    mode: 'create' | 'rename',
    initialName = '',
  ): Promise<{ fixture: ComponentFixture<CategoryFormDialogComponent>; saved: string[] }> {
    await TestBed.configureTestingModule({ imports: [CategoryFormDialogComponent] }).compileComponents();
    const fixture = TestBed.createComponent(CategoryFormDialogComponent);
    const saved: string[] = [];
    fixture.componentInstance.save.subscribe((value) => saved.push(value));
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('initialName', initialName);
    fixture.componentRef.setInput('visible', true);
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, saved };
  }

  function type(fixture: ComponentFixture<CategoryFormDialogComponent>, value: string): void {
    const input = (fixture.nativeElement as HTMLElement).querySelector('input');
    if (!input) {
      throw new Error('name input not rendered');
    }

    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function clickSave(fixture: ComponentFixture<CategoryFormDialogComponent>): void {
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
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('اسم التصنيف مطلوب');
  });

  it('a valid name is emitted trimmed', async () => {
    const { fixture, saved } = await open('create');

    type(fixture, '  أدوية  ');
    clickSave(fixture);

    expect(saved).toEqual(['أدوية']);
  });

  it('rename mode pre-fills the current name and uses the rename title', async () => {
    const { fixture } = await open('rename', 'أدوية');

    const input = (fixture.nativeElement as HTMLElement).querySelector('input');
    expect(input?.value).toBe('أدوية');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('إعادة تسمية التصنيف');
  });

  it('a duplicate-name failure projects inline onto the field — not a banner — and clears on edit (STD-UX-019/020)', async () => {
    const { fixture } = await open('create');

    type(fixture, 'أدوية');
    fixture.componentRef.setInput('serverFailure', {
      kind: 'field',
      code: 'VTF-VAL-001',
      messageKey: 'errors.VTF-VAL-001',
      retryable: false,
      fieldErrors: { name: ['server text (never rendered)'] },
    } satisfies ClassifiedFailure);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('يوجد تصنيف بهذا الاسم بالفعل');
    expect((fixture.nativeElement as HTMLElement).querySelector('vf-banner')).toBeNull();

    type(fixture, 'أدوية جديدة');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('يوجد تصنيف بهذا الاسم بالفعل');
  });

  it('a non-field failure renders as the dialog’s own operation message (STD-UX-080/082)', async () => {
    const { fixture } = await open('create');

    fixture.componentRef.setInput('serverFailure', {
      kind: 'system',
      code: null,
      messageKey: 'categories.error.saveFailed',
      retryable: false,
      fieldErrors: null,
    } satisfies ClassifiedFailure);
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
    expect(banner?.textContent).toContain('تعذّر حفظ التصنيف. أعد المحاولة.');
    expect(banner?.getAttribute('role')).toBe('alert');
  });
});
