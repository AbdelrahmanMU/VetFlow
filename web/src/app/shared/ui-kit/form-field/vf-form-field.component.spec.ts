import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { vfValidators } from '../../../core/validation/validators';
import { VfTextInputComponent } from '../input/vf-text-input.component';
import { VfFormFieldComponent } from './vf-form-field.component';
import { VfValidationSummaryComponent } from './vf-validation-summary.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfFormFieldComponent,
    VfTextInputComponent,
    VfValidationSummaryComponent,
  ],
  template: `
    <form [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="submitted = true">
      <vf-validation-summary />
      <vf-form-field label="اسم التصنيف" [required]="true" hint="مثال: أدوية بيطرية">
        <vf-text-input [formControl]="form.controls.name" />
      </vf-form-field>
      <button type="submit">حفظ</button>
    </form>
  `,
})
class HostComponent {
  readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [vfValidators.required, vfValidators.maxLength(5)],
    }),
  });
  submitted = false;
}

async function create() {
  await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
  const fixture = TestBed.createComponent(HostComponent);
  fixture.detectChanges();
  const root = fixture.nativeElement as HTMLElement;
  const input = root.querySelector<HTMLInputElement>('.field-input');
  const formEl = root.querySelector<HTMLFormElement>('form');
  if (!input || !formEl) {
    throw new Error('host not rendered');
  }

  return { fixture, host: fixture.componentInstance, root, input, formEl };
}

const flush = () => new Promise((resolve) => setTimeout(resolve, 0));

describe('VfFormFieldComponent', () => {
  it('wires label, id, and aria-describedby — no error before any moment (hint shows first)', async () => {
    const { root, input } = await create();

    const label = root.querySelector<HTMLLabelElement>('.vf-field-label');
    expect(label?.htmlFor).toBe(input.id);
    expect(input.getAttribute('aria-describedby')).toBe(`${input.id}-message`);
    expect(root.querySelector('.vf-msg-error')).toBeNull();
    expect(root.querySelector('.vf-msg-hint')?.textContent?.trim()).toBe('مثال: أدوية بيطرية');
    expect(input.getAttribute('aria-invalid')).toBe('false');
  });

  it('moment 2: blur reveals the field error, replacing the hint (STD-UX-011)', async () => {
    const { fixture, root, input } = await create();

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(root.querySelector('.vf-msg-error')?.textContent).toContain('هذا الحقل مطلوب.');
    expect(root.querySelector('.vf-msg-hint')).toBeNull();
    expect(input.getAttribute('aria-invalid')).toBe('true');
  });

  it('the error disappears on the input event that fixes it, and success appears (STD-UX-014/015)', async () => {
    const { fixture, root, input } = await create();

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    expect(root.querySelector('.vf-msg-error')).not.toBeNull();

    input.value = 'أدوية';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(root.querySelector('.vf-msg-error')).toBeNull();
    expect(root.querySelector('.vf-msg-success')).not.toBeNull();
    expect(input.getAttribute('aria-invalid')).toBe('false');
  });

  it('the message switches when the violated rule changes while the field stays invalid', async () => {
    const { fixture, root, input } = await create();

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    expect(root.querySelector('.vf-msg-error')?.textContent).toContain('هذا الحقل مطلوب.');

    // required → maxlength: still invalid, but a different rule — the
    // sentence must follow (the frozen-computed regression found in the
    // Phase 1 browser verification).
    input.value = 'اسم أطول من الحد';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(root.querySelector('.vf-msg-error')?.textContent).toContain('يجب ألّا يتجاوز هذا الحقل 5 حرفًا.');
  });

  it('moment 3: a rejected submit reveals errors, shows the summary, and focuses the first invalid control', async () => {
    const { fixture, host, root, input, formEl } = await create();

    formEl.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await flush();
    fixture.detectChanges();

    expect(host.submitted).toBe(false);
    expect(root.querySelector('.vf-msg-error')).not.toBeNull();
    const summaryLink = root.querySelector<HTMLButtonElement>('.vf-summary-link');
    expect(summaryLink?.textContent?.trim()).toBe('اسم التصنيف');
    expect(document.activeElement).toBe(input);
  });

  it('a summary link navigates to its field (STD-UX-076)', async () => {
    const { fixture, root, input, formEl } = await create();

    formEl.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await flush();

    (document.activeElement as HTMLElement | null)?.blur();
    const summaryLink = root.querySelector<HTMLButtonElement>('.vf-summary-link');
    summaryLink?.click();
    expect(document.activeElement).toBe(input);
  });

  it('a valid submit emits and shows no summary', async () => {
    const { fixture, host, root, input, formEl } = await create();

    input.value = 'أدوية';
    input.dispatchEvent(new Event('input'));
    formEl.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.submitted).toBe(true);
    expect(root.querySelector('.vf-summary')).toBeNull();
  });

  it('a projected server error renders inline and clears on the next edit (STD-UX-015/019)', async () => {
    const { fixture, host, root, input } = await create();

    host.form.controls.name.setValue('مكرر');
    host.form.controls.name.setErrors({ server: 'categories.error.duplicate' });
    host.form.controls.name.markAsTouched();
    fixture.detectChanges();
    expect(root.querySelector('.vf-msg-error')?.textContent).toContain(
      'يوجد تصنيف بهذا الاسم بالفعل.',
    );

    input.value = 'صحيح';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(root.querySelector('.vf-msg-error')).toBeNull();
  });
});
