import { Injectable } from '@angular/core';

/**
 * Locale-sensitive formatting isolated behind one service (STD-FE-042,
 * ADR-0007): Egyptian Arabic conventions with Western digits, Gregorian
 * calendar, EGP currency.
 */
@Injectable({ providedIn: 'root' })
export class FormatService {
  private static readonly Locale = 'ar-EG-u-nu-latn';

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
   */
  date(isoDate: string): string {
    const [year, month, day] = isoDate.split('-').map(Number);
    if (!year || !month || !day) {
      return isoDate;
    }

    return this.dateFormat.format(new Date(year, month - 1, day));
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
      return isoTimestamp;
    }

    return this.dateTimeFormat.format(parsed);
  }

  /**
   * The same instant as {@link dateTime}, split into its two lines: the date above
   * the time (owner ruling, 2026-08-02). One definition, so no screen decides its
   * own date presentation — the timestamp is parsed once here and the parts are
   * formatted by the same locale as everything else, meridiem included (ص/م).
   *
   * An unparseable value degrades exactly as {@link dateTime} does: the raw string
   * on the date line and nothing on the time line, never `Invalid Date`.
   */
  dateTimeParts(isoTimestamp: string): { readonly date: string; readonly time: string } {
    const parsed = new Date(isoTimestamp);
    if (Number.isNaN(parsed.getTime())) {
      return { date: isoTimestamp, time: '' };
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
}
