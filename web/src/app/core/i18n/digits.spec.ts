import { normalizeDigits } from './digits';

/**
 * Regression contract for the canonical digit normalizer (Pilot P1 / F1 —
 * money integrity). The four acceptance examples ARE the contract; the rest
 * guard the money-correctness invariant: valid → the exact number, invalid →
 * a string Number() rejects, never a different plausible number.
 */
describe('normalizeDigits', () => {
  it('maps pure Arabic-Indic digits to Latin (٥٠٠ → 500)', () => {
    expect(normalizeDigits('٥٠٠')).toBe('500');
    expect(Number(normalizeDigits('٥٠٠'))).toBe(500);
  });

  it('maps mixed Arabic-Indic and Latin digits (5٠٠ → 500)', () => {
    expect(normalizeDigits('5٠٠')).toBe('500');
    expect(Number(normalizeDigits('5٠٠'))).toBe(500);
  });

  it('maps interleaved mixed digits (١2٣٤ → 1234)', () => {
    expect(normalizeDigits('١2٣٤')).toBe('1234');
    expect(Number(normalizeDigits('١2٣٤'))).toBe(1234);
  });

  it('preserves the ASCII decimal point across mixed digits (12٣.٥٠ → 123.50)', () => {
    expect(normalizeDigits('12٣.٥٠')).toBe('123.50');
    expect(Number(normalizeDigits('12٣.٥٠'))).toBe(123.5);
  });

  it('maps extended Arabic-Indic (Persian) digits (۱۲۳ → 123)', () => {
    expect(normalizeDigits('۱۲۳')).toBe('123');
    expect(Number(normalizeDigits('۱۲۳٫۵'))).toBe(123.5);
  });

  it('maps the Arabic decimal separator ٫ to an ASCII point', () => {
    expect(normalizeDigits('١٢٣٫٥٠')).toBe('123.50');
    expect(Number(normalizeDigits('١٢٣٫٥٠'))).toBe(123.5);
  });

  it('drops the Arabic thousands separator ٬ (grouping)', () => {
    expect(normalizeDigits('١٬٢٣٤')).toBe('1234');
    expect(Number(normalizeDigits('١٬٢٣٤'))).toBe(1234);
  });

  it('strips bidi / zero-width marks that ride along on a copy-paste from RTL text', () => {
    // RLM + Arabic-Indic 500 + RLM, as copied out of an Arabic document.
    const pasted = '‏٥٠٠‏';
    expect(normalizeDigits(pasted)).toBe('500');
    expect(Number(normalizeDigits(pasted))).toBe(500);
    // A zero-width space wedged between digits must not break the number.
    expect(Number(normalizeDigits('٥​٠٠'))).toBe(500);
  });

  it('leaves surrounding whitespace for Number() to trim (paste with spaces)', () => {
    expect(Number(normalizeDigits('  ٥٠٠  '))).toBe(500);
  });

  it('never turns invalid input into a plausible number — it stays NaN', () => {
    expect(Number.isNaN(Number(normalizeDigits('abc')))).toBe(true);
    expect(Number.isNaN(Number(normalizeDigits('١٢ريال')))).toBe(true);
    // A Latin grouping comma is out of F1 scope: it must fail loudly (→ NaN,
    // then null at the boundary), never silently become a different amount.
    expect(Number.isNaN(Number(normalizeDigits('1,234.50')))).toBe(true);
  });

  it('is a no-op on already-canonical Latin input', () => {
    expect(normalizeDigits('123.50')).toBe('123.50');
    expect(normalizeDigits('')).toBe('');
  });
});
