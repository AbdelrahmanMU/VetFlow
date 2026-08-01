import { FormControl, FormGroup } from '@angular/forms';

import { projectServerFieldErrors } from './server-errors';

describe('projectServerFieldErrors', () => {
  it('maps matched fields onto their controls and returns the unmatched keys — nothing dropped (STD-UX-019)', () => {
    const form = new FormGroup({
      name: new FormControl('مضاد حيوي', { nonNullable: true }),
    });

    const unmatched = projectServerFieldErrors(
      form,
      { name: ['server text (never rendered)'], somethingElse: ['x'] },
      { name: 'categories.error.duplicate' },
    );

    expect(form.controls.name.errors).toEqual({ server: 'categories.error.duplicate' });
    expect(form.controls.name.touched).toBe(true);
    expect(unmatched).toEqual(['somethingElse']);
  });

  it('preserves the control’s existing rule errors', () => {
    const form = new FormGroup({
      name: new FormControl('', { nonNullable: true }),
    });
    form.controls.name.setErrors({ required: true });

    projectServerFieldErrors(form, { name: ['x'] }, { name: 'categories.error.duplicate' });

    expect(form.controls.name.errors).toEqual({
      required: true,
      server: 'categories.error.duplicate',
    });
  });
});
