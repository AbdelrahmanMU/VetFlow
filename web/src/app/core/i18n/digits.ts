/**
 * Canonical digit normalization — the SINGLE algorithm for turning
 * human-entered numeric text into a canonical Latin-digit string that
 * `Number()` can parse. Every numeric input (money above all) funnels through
 * here, so a value typed or pasted with Arabic-Indic (٠١٢٣), Persian (۰۱۲۳),
 * or mixed digits can never produce a wrong monetary amount.
 *
 * Correctness guarantee (money integrity): a valid number written in any
 * supported digit script normalizes to the exact same number in Latin digits;
 * anything that is not a number normalizes to a string `Number()` rejects
 * (→ null at the input boundary) — never a different, plausible number.
 *
 * Do not add a second normalizer. If another path needs digit handling, call
 * this function.
 */

/** ٠ .. ٩  Arabic-Indic digits (U+0660–U+0669). */
const ARABIC_INDIC_ZERO = 0x0660;

/** ۰ .. ۹  Extended Arabic-Indic / Persian digits (U+06F0–U+06F9). */
const EXTENDED_ARABIC_INDIC_ZERO = 0x06f0;

/** Arabic decimal separator ٫ (U+066B) — maps to the ASCII point. */
const ARABIC_DECIMAL_SEPARATOR = 0x066b;

/** Arabic thousands separator ٬ (U+066C) — grouping only, dropped. */
const ARABIC_THOUSANDS_SEPARATOR = 0x066c;

/**
 * Bidi / zero-width format marks that ride along when numeric text is copied
 * out of a right-to-left document. `Number()` treats them as NaN, so an
 * otherwise-valid pasted amount would silently become null — drop them:
 * LRM, RLM, ALM, ZWSP, ZWNJ, ZWJ, BOM/ZWNBSP.
 */
const FORMAT_MARKS = new Set<number>([
  0x200e, 0x200f, 0x061c, 0x200b, 0x200c, 0x200d, 0xfeff,
]);

function isBetween(code: number, start: number): boolean {
  return code >= start && code <= start + 9;
}

/**
 * Normalize digit scripts and Arabic separators to a canonical Latin form.
 * Whitespace is left as-is — `Number()` already trims it at the boundary.
 */
export function normalizeDigits(input: string): string {
  let out = '';

  for (const ch of input) {
    const code = ch.codePointAt(0) ?? 0;

    if (FORMAT_MARKS.has(code)) {
      continue;
    } else if (isBetween(code, ARABIC_INDIC_ZERO)) {
      out += String.fromCharCode(0x30 + (code - ARABIC_INDIC_ZERO));
    } else if (isBetween(code, EXTENDED_ARABIC_INDIC_ZERO)) {
      out += String.fromCharCode(0x30 + (code - EXTENDED_ARABIC_INDIC_ZERO));
    } else if (code === ARABIC_DECIMAL_SEPARATOR) {
      out += '.';
    } else if (code === ARABIC_THOUSANDS_SEPARATOR) {
      continue;
    } else {
      out += ch;
    }
  }

  return out;
}
