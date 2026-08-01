import { FormControl } from '@angular/forms';

import { vfValidators } from './validators';

describe('vfValidators', () => {
  it('positive: rejects zero and negatives, passes empty (compose with required) and > 0', () => {
    expect(vfValidators.positive(new FormControl<number | null>(null))).toBeNull();
    expect(vfValidators.positive(new FormControl<number | null>(5))).toBeNull();
    expect(vfValidators.positive(new FormControl<number | null>(0))).toEqual({ positive: true });
    expect(vfValidators.positive(new FormControl<number | null>(-3))).toEqual({ positive: true });
  });

  it('wholeNumber: rejects fractions, passes integers and empty', () => {
    expect(vfValidators.wholeNumber(new FormControl<number | null>(null))).toBeNull();
    expect(vfValidators.wholeNumber(new FormControl<number | null>(4))).toBeNull();
    expect(vfValidators.wholeNumber(new FormControl<number | null>(2.5))).toEqual({
      wholeNumber: true,
    });
  });
});
