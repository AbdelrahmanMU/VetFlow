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

  describe('dateTimeParts — the two-line stamp (owner ruling, 2026-08-02)', () => {
    it('splits the instant into a date line and a time line', () => {
      const parts = format.dateTimeParts('2026-07-31T14:35:00+00:00');

      // The date line is exactly what date() renders for the same day, so the two
      // presentations can never drift apart.
      expect(parts.date).toBe(format.date('2026-07-31'));
      expect(parts.time).not.toBe('');
      expect(parts.time).not.toContain('2026');
      // Together they carry no more and no less than the single-line form.
      expect(parts.date).not.toContain(':');
      expect(parts.time).toMatch(/\d/);
    });

    it('keeps the Arabic meridiem rather than switching the UI to AM/PM', () => {
      const morning = format.dateTimeParts('2026-07-31T06:00:00Z').time;
      const evening = format.dateTimeParts('2026-07-31T18:00:00Z').time;

      // ar-EG renders ص/م natively; the ruling kept it (the owner's AM/PM example
      // was read as illustrating the two-line layout, not the language).
      expect(`${morning}${evening}`).not.toMatch(/AM|PM/);
      expect(morning).not.toBe(evening);
    });

    it('degrades like dateTime() instead of rendering "Invalid Date"', () => {
      expect(format.dateTimeParts('not-a-timestamp')).toEqual({ date: 'not-a-timestamp', time: '' });
    });
  });
});
