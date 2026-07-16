/** Contract types of the manufacturer management endpoints (REQ-CAT-047, ADR-0015). */

export interface ManufacturerListItem {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
}

export type ManufacturerSortField = 'name' | 'status';

export interface ManufacturerSort {
  readonly field: ManufacturerSortField;
  readonly direction: 'asc' | 'desc';
}

export interface ManufacturerListRequest {
  readonly search: string;
  readonly sort: ManufacturerSort;
  readonly page: number;
  readonly pageSize: number;
}
