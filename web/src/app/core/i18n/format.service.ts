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

  integer(value: number): string {
    return this.integerFormat.format(value);
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
