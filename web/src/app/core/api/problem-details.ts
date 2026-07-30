/** RFC 9457 Problem Details — the API's single error shape (ADR-0015 §3). */
export interface ProblemDetails {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errorCode?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, readonly string[]>;
  /** Structured facts about the failure — e.g. the products a sale could not cover. Data, not copy. */
  readonly metadata?: Record<string, string>;
}

/**
 * A failed API call. Client logic branches on {@link errorCode} only —
 * never on message text (STD-FE-037).
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails | null,
  ) {
    super(problem?.errorCode ?? `HTTP ${status}`);
    this.name = 'ApiError';
  }

  get errorCode(): string | null {
    return this.problem?.errorCode ?? null;
  }
}
