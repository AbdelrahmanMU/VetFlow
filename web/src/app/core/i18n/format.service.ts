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

  integer(value: number): string {
    return this.integerFormat.format(value);
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
