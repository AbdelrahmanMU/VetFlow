import { Injectable } from '@angular/core';

/**
 * Locale-sensitive formatting isolated behind one service (STD-FE-042,
 * ADR-0007): Egyptian Arabic conventions with Western digits, Gregorian
 * calendar, EGP currency.
 */
@Injectable({ providedIn: 'root' })
export class FormatService {
  private static readonly Locale = 'ar-EG-u-nu-latn';

  /** What an unrenderable value shows as — never machine text (design language §10). */
  private static readonly Unavailable = '—';

  private readonly integerFormat = new Intl.NumberFormat(FormatService.Locale, {
    maximumFractionDigits: 0,
  });

  private readonly currencyFormats = new Map<string, Intl.NumberFormat>();

  private readonly dateFormat = new Intl.DateTimeFormat(FormatService.Locale, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });

  private readonly dateTimeFormat = new Intl.DateTimeFormat(FormatService.Locale, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });

  private readonly timeFormat = new Intl.DateTimeFormat(FormatService.Locale, {
    hour: '2-digit',
    minute: '2-digit',
  });

  private readonly decimalFormats = new Map<number, Intl.NumberFormat>();

  private readonly moneyAmountFormat = new Intl.NumberFormat(FormatService.Locale, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  integer(value: number): string {
    return this.integerFormat.format(value);
  }

  /** A decimal amount (e.g. a line quantity) with up to `maxFractionDigits` places. */
  decimal(value: number, maxFractionDigits = 3): string {
    let format = this.decimalFormats.get(maxFractionDigits);
    if (!format) {
      format = new Intl.NumberFormat(FormatService.Locale, { maximumFractionDigits: maxFractionDigits });
      this.decimalFormats.set(maxFractionDigits, format);
    }

    return format.format(value);
  }

  /**
   * Formats an ISO `yyyy-mm-dd` date-only string. The parts are read directly and
   * a local Date is built so the displayed day always equals the stored day — never
   * shifted by a timezone (`new Date('yyyy-mm-dd')` would parse as UTC midnight).
   *
   * <b>It still refuses an instant</b> — a timestamp is not a business date, and
   * deriving one from it in the browser is BR-INV-059/060's decision, not ours. What
   * changed on 2026-08-06 is only the <i>degrade</i>: a refused value now renders as
   * a dash instead of being echoed back, because echoing put
   * `2026-08-02T22:17:31.25352+00:00` on the screen of a real clinic. Machine text
   * never reaches the user (design language §10). Use {@link dateOfInstant} for a
   * timestamp.
   */
  date(isoDate: string): string {
    const [year, month, day] = isoDate.split('-').map(Number);
    if (!year || !month || !day) {
      return FormatService.Unavailable;
    }

    return this.dateFormat.format(new Date(year, month - 1, day));
  }

  /**
   * The date line of an instant — {@link dateTimeParts} without the time, for the
   * columns that record *when a row was created or received* rather than a business
   * date the clinic chose.
   *
   * One definition, so those columns cannot drift from the two-line stamp: this is
   * literally `dateTimeParts().date`. It replaces the `createdAt.slice(0, 10)` that
   * four screens each wrote by hand — which read the day off the raw UTC text and so
   * could name a different day than the stamp beside it.
   */
  dateOfInstant(isoTimestamp: string): string {
    return this.dateTimeParts(isoTimestamp).date;
  }

  /**
   * Formats a full ISO timestamp (an instant, e.g. `2026-07-31T02:31:28+00:00`) as date + time in
   * the viewer's local zone.
   *
   * <b>Distinct from {@link date} on purpose.</b> `date` parses `yyyy-mm-dd` by splitting on `-`,
   * so handing it a timestamp yields `NaN` parts and it returns the raw machine string — which is
   * exactly what the movement history did before this existed. A timestamp needs its own parse.
   *
   * This formats an *instant*, not a business date: it must never be used for an expiry or any
   * other clinic-local business date, which BR-INV-059/060 govern and the server decides.
   */
  dateTime(isoTimestamp: string): string {
    const parsed = new Date(isoTimestamp);
    if (Number.isNaN(parsed.getTime())) {
      return FormatService.Unavailable;
    }

    return this.dateTimeFormat.format(parsed);
  }

  /**
   * The same instant as {@link dateTime}, split into its two lines: the date above
   * the time (owner ruling, 2026-08-02). One definition, so no screen decides its
   * own date presentation — the timestamp is parsed once here and the parts are
   * formatted by the same locale as everything else, meridiem included (ص/م).
   *
   * An unparseable value degrades exactly as {@link dateTime} does: a dash on the
   * date line and nothing on the time line — never `Invalid Date`, and never the raw
   * machine string.
   */
  dateTimeParts(isoTimestamp: string): { readonly date: string; readonly time: string } {
    const parsed = new Date(isoTimestamp);
    if (Number.isNaN(parsed.getTime())) {
      return { date: FormatService.Unavailable, time: '' };
    }

    return { date: this.dateFormat.format(parsed), time: this.timeFormat.format(parsed) };
  }

  money(amount: number, currency: string): string {
    let format = this.currencyFormats.get(currency);
    if (!format) {
      format = new Intl.NumberFormat(FormatService.Locale, {
        style: 'currency',
        currency,
        currencyDisplay: 'symbol',
      });
      this.currencyFormats.set(currency, format);
    }

    return format.format(amount);
  }

  /**
   * A money amount with no symbol, at the same precision {@link money} renders —
   * for the one read contract that carries an amount without a currency code (the
   * batch unit-cost snapshot, which pairs with its own approved «ج.م.» label).
   *
   * It exists so that screen stops reaching for {@link decimal}: at up to three
   * fraction digits, the same 12.50 showed as «12.5» there and «12.50» everywhere
   * else, and two precisions in one product is exactly what §10's single money
   * presentation forbids. No currency is invented here — only the digits are.
   */
  moneyAmount(amount: number): string {
    return this.moneyAmountFormat.format(amount);
  }
}
