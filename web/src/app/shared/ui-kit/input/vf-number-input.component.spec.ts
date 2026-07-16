import { TestBed } from '@angular/core/testing';

import { VfNumberInputComponent } from './vf-number-input.component';

/**
 * Money-integrity regression at the input boundary (Pilot P1 / F1). This is the
 * single component every monetary value is typed into; it must funnel raw text
 * through the canonical normalizer so mixed Arabic-Indic / Latin digits can
 * never be recorded as a wrong amount.
 */
describe('VfNumberInputComponent', () => {
  async function create() {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [VfNumberInputComponent] }).compileComponents();
    const fixture = TestBed.createComponent(VfNumberInputComponent);
    fixture.detectChanges();
    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.field-input');
    if (!input) {
      throw new Error('input not rendered');
    }

    return { fixture, component: fixture.componentInstance, input };
  }

  async function type(value: string) {
    const { fixture, component, input } = await create();
    let captured: number | null | undefined;
    component.registerOnChange((v) => (captured = v));
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges(); // flush the canonical text back into the DOM (zoneless CD is async)
    return { captured, input };
  }

  it('renders a text field with a decimal keypad, not a native number input', async () => {
    const { input } = await create();
    // type="number" would blank Arabic-Indic input before any script sees it.
    expect(input.getAttribute('type')).toBe('text');
    expect(input.getAttribute('inputmode')).toBe('decimal');
  });

  it.each([
    ['٥٠٠', 500],
    ['5٠٠', 500],
    ['١2٣٤', 1234],
    ['12٣.٥٠', 123.5],
  ])('records "%s" as %d (the acceptance examples)', async (entered, expected) => {
    const { captured } = await type(entered);
    expect(captured).toBe(expected);
  });

  it('records a value pasted with surrounding whitespace and RTL marks', async () => {
    const { captured } = await type('‏  ٥٠٠  ‏');
    expect(captured).toBe(500);
  });

  it('reflects the canonical digits back into the field (٥٠٠ shown as 500)', async () => {
    const { input } = await type('٥٠٠');
    expect(input.value).toBe('500');
  });

  it('keeps an in-progress decimal typeable ("123." stays "123." → 123)', async () => {
    const { captured, input } = await type('123.');
    expect(input.value).toBe('123.');
    expect(captured).toBe(123);
  });

  it('records invalid input as null rather than a wrong number', async () => {
    expect((await type('abc')).captured).toBeNull();
    expect((await type('')).captured).toBeNull();
    expect((await type('1,234.50')).captured).toBeNull();
  });

  it('reflects a form-written value into the field (edit prefill)', async () => {
    const { fixture, component, input } = await create();
    component.writeValue(123.5);
    fixture.detectChanges();
    expect(input.value).toBe('123.5');
    component.writeValue(null);
    fixture.detectChanges();
    expect(input.value).toBe('');
  });
});
