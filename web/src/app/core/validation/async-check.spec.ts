import { Subject, of, throwError } from 'rxjs';

import { debouncedCheck } from './async-check';

describe('debouncedCheck', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('waits for the typing pause and respects the minimum length (STD-UX-101)', () => {
    const calls: string[] = [];
    const results: string[] = [];
    const input$ = new Subject<string>();
    input$
      .pipe(
        debouncedCheck({
          check: (value: string) => (calls.push(value), of(`${value}!`)),
          minLength: 2,
        }),
      )
      .subscribe((result) => results.push(result));

    input$.next('a');
    vi.advanceTimersByTime(300);
    expect(calls).toEqual([]);

    input$.next('ab');
    vi.advanceTimersByTime(299);
    expect(calls).toEqual([]);
    vi.advanceTimersByTime(1);
    expect(calls).toEqual(['ab']);
    expect(results).toEqual(['ab!']);
  });

  it('caches per entered value — the same value never checks twice (STD-UX-102)', () => {
    const calls: string[] = [];
    const results: string[] = [];
    const input$ = new Subject<string>();
    input$
      .pipe(debouncedCheck({ check: (value: string) => (calls.push(value), of(`${value}!`)) }))
      .subscribe((result) => results.push(result));

    input$.next('ab');
    vi.advanceTimersByTime(300);
    input$.next('abc');
    vi.advanceTimersByTime(300);
    input$.next('ab');
    vi.advanceTimersByTime(300);

    expect(calls).toEqual(['ab', 'abc']);
    expect(results).toEqual(['ab!', 'abc!', 'ab!']);
  });

  it('a failed advisory check emits nothing and never kills the stream', () => {
    const results: string[] = [];
    const input$ = new Subject<string>();
    input$
      .pipe(
        debouncedCheck({
          check: (value: string) =>
            value === 'xx' ? throwError(() => new Error('down')) : of(`${value}!`),
        }),
      )
      .subscribe((result) => results.push(result));

    input$.next('xx');
    vi.advanceTimersByTime(300);
    expect(results).toEqual([]);

    input$.next('cd');
    vi.advanceTimersByTime(300);
    expect(results).toEqual(['cd!']);
  });
});
