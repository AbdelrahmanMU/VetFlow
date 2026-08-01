import { EMPTY, Observable, OperatorFunction, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  switchMap,
  tap,
} from 'rxjs/operators';

/**
 * Debounced, cancelling, cached advisory check (validation-and-guidance.md
 * §11). Guarantees STD-UX-101/102: waits for a typing pause, never issues a
 * call per keystroke, cancels superseded requests (switch semantics), and
 * caches per entered value for the life of the stream.
 *
 * Advisory only: a failed check emits nothing rather than erroring the
 * stream — an advisory check never blocks the user (BR-CAT-042 spirit); the
 * definitive rule is enforced at submit by the server (STD-UX-008).
 */
export interface DebouncedCheckOptions<TInput, TResult> {
  readonly check: (input: TInput) => Observable<TResult>;
  /** STD-UX-101: default 300 ms; a different value needs a recorded reason. */
  readonly debounceMs?: number;
  /** Minimum length before a string input is checked (STD-UX-101). */
  readonly minLength?: number;
  /** Cache key for an input; defaults to `JSON.stringify`. */
  readonly keyOf?: (input: TInput) => string;
}

export function debouncedCheck<TInput, TResult>(
  options: DebouncedCheckOptions<TInput, TResult>,
): OperatorFunction<TInput, TResult> {
  const cache = new Map<string, TResult>();
  const keyOf = options.keyOf ?? ((input: TInput): string => JSON.stringify(input));

  return (input$: Observable<TInput>): Observable<TResult> =>
    input$.pipe(
      debounceTime(options.debounceMs ?? 300),
      filter((input) => typeof input !== 'string' || input.length >= (options.minLength ?? 0)),
      distinctUntilChanged((previous, next) => keyOf(previous) === keyOf(next)),
      switchMap((input) => {
        const key = keyOf(input);
        const hit = cache.get(key);
        if (hit !== undefined) {
          return of(hit);
        }

        return options.check(input).pipe(
          tap((result) => cache.set(key, result)),
          catchError(() => EMPTY),
        );
      }),
    );
}
