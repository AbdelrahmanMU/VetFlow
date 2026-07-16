/** Contract types of the category management endpoints (REQ-CTG-001, ADR-0015). */

export interface CategoryListItem {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
}

export type CategorySortField = 'name' | 'status';

export interface CategorySort {
  readonly field: CategorySortField;
  readonly direction: 'asc' | 'desc';
}

export interface CategoryListRequest {
  readonly search: string;
  readonly sort: CategorySort;
  readonly page: number;
  readonly pageSize: number;
}
