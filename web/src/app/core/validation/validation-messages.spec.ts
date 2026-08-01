import { resolveValidationMessage } from './validation-messages';

describe('resolveValidationMessage', () => {
  it('returns null for a clean control', () => {
    expect(resolveValidationMessage(null)).toBeNull();
  });

  it('resolves the shared defaults — one rule, one sentence (STD-UX-017)', () => {
    expect(resolveValidationMessage({ required: true })).toEqual({
      key: 'validation.required',
      params: undefined,
    });
    expect(resolveValidationMessage({ positive: true })).toEqual({
      key: 'validation.positive',
      params: undefined,
    });
    expect(resolveValidationMessage({ wholeNumber: true })).toEqual({
      key: 'validation.wholeNumber',
      params: undefined,
    });
  });

  it('gives max-length its own sentence with the limit as a param — never the required copy', () => {
    expect(
      resolveValidationMessage({ maxlength: { requiredLength: 100, actualLength: 130 } }),
    ).toEqual({
      key: 'validation.maxLength',
      params: { max: 100 },
    });
  });

  it('prefers a ruled contextual override (STD-UX-111)', () => {
    expect(
      resolveValidationMessage(
        { required: true },
        { required: 'adjustment.error.productRequired' },
      ),
    ).toEqual({ key: 'adjustment.error.productRequired', params: undefined });
  });

  it('a projected server error always wins (STD-UX-019)', () => {
    expect(
      resolveValidationMessage(
        { server: 'categories.error.duplicate', required: true },
        { required: 'categories.error.required' },
      ),
    ).toEqual({ key: 'categories.error.duplicate' });
  });

  it('an unmapped rule falls back to the guided generic sentence, never blank', () => {
    expect(resolveValidationMessage({ somethingCustom: true })).toEqual({
      key: 'validation.invalid',
    });
  });
});
