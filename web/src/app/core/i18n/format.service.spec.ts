import { TestBed } from '@angular/core/testing';

import { FormatService } from './format.service';

describe('FormatService', () => {
  let format: FormatService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    format = TestBed.inject(FormatService);
  });

  it('formats a date-only string without shifting the day across a timezone', () => {
    expect(format.date('2026-07-31')).toContain('2026');
    expect(format.date('2026-07-31')).toContain('31');
  });

  it('formats a full ISO timestamp instead of echoing it back', () => {
    // The regression this method exists for: `date()` splits on "-", so a timestamp made its
    // third part NaN and the raw machine string reached the screen — which is what the movement
    // history rendered in the browser before `dateTime()` existed.
    const rendered = format.dateTime('2026-07-31T02:31:28.451294+00:00');

    expect(rendered).not.toContain('T');
    expect(rendered).not.toContain('+00:00');
    expect(rendered).toContain('2026');
  });

  it('echoes an unparseable timestamp rather than rendering "Invalid Date"', () => {
    expect(format.dateTime('not-a-timestamp')).toBe('not-a-timestamp');
  });

  it('date() still refuses a timestamp, which is why dateTime() is separate', () => {
    // Documents the boundary deliberately: date() is for clinic-local business dates
    // (BR-INV-059/060) and must not be fed an instant.
    expect(format.date('2026-07-31T02:31:28+00:00')).toBe('2026-07-31T02:31:28+00:00');
  });
});
