import { TestBed } from '@angular/core/testing';

import { VfTextInputComponent } from './vf-text-input.component';

describe('VfTextInputComponent', () => {
  async function create() {
    await TestBed.configureTestingModule({ imports: [VfTextInputComponent] }).compileComponents();
    const fixture = TestBed.createComponent(VfTextInputComponent);
    fixture.detectChanges();
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.field-input');
    if (!input) {
      throw new Error('input not rendered');
    }

    return { fixture, component: fixture.componentInstance, input };
  }

  it('reflects a written value and reports edits back to the form (ControlValueAccessor)', async () => {
    const { fixture, component, input } = await create();
    let captured = '';
    component.registerOnChange((value) => (captured = value));

    component.writeValue('أموكسي');
    fixture.detectChanges();
    expect(input.value).toBe('أموكسي');

    input.value = 'أموكسيسيلين';
    input.dispatchEvent(new Event('input'));
    expect(captured).toBe('أموكسيسيلين');
  });

  it('renders its error line when an error is provided (STD-FE-017)', async () => {
    const { fixture } = await create();
    fixture.componentRef.setInput('error', 'هذا الحقل مطلوب.');
    fixture.detectChanges();

    const error = (fixture.nativeElement as HTMLElement).querySelector('.field-error');
    expect(error?.textContent?.trim()).toBe('هذا الحقل مطلوب.');
  });
});
