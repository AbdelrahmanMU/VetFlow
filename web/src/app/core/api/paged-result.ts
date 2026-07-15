/** The fixed collection envelope of the API contract (ADR-0015 §5). */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}
