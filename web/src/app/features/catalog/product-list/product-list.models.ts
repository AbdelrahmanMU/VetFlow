/** Contract types of GET /api/v1/products and the lookup endpoints (ADR-0015). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

export type ProductStatus = 'active' | 'disabled';

export interface ProductListItem {
  readonly id: string;
  readonly arabicName: string;
  readonly englishName: string | null;
  readonly size: string | null;
  readonly concentration: string | null;
  readonly categoryName: string;
  readonly manufacturerName: string;
  readonly natureName: string;
  readonly storageUnitName: string;
  readonly defaultSaleUnitName: string;
  readonly defaultSaleUnitPrice: Money | null;
  readonly status: ProductStatus;
  readonly isSplittable: boolean;
  readonly isRefrigerated: boolean;
  readonly hasExpiration: boolean;
  readonly hasSellingPrice: boolean;
}

export interface LookupOption {
  readonly id: string;
  readonly name: string;
}

/** The Catalog-owned list filters (BR-CAT-045); stock filters ship with Inventory/Monitoring. */
export interface ProductFilters {
  readonly category: LookupOption | null;
  readonly manufacturer: LookupOption | null;
  readonly nature: LookupOption | null;
  readonly status: ProductStatus | null;
  readonly refrigerated: boolean | null;
  readonly hasExpiration: boolean | null;
  readonly splittable: boolean | null;
  readonly hasSellingPrice: boolean | null;
}

export const EMPTY_FILTERS: ProductFilters = {
  category: null,
  manufacturer: null,
  nature: null,
  status: null,
  refrigerated: null,
  hasExpiration: null,
  splittable: null,
  hasSellingPrice: null,
};

export type ProductSortField = 'name' | 'category' | 'manufacturer' | 'nature' | 'price' | 'status';

export interface ProductSort {
  readonly field: ProductSortField;
  readonly direction: 'asc' | 'desc';
}

export interface ProductListRequest {
  readonly search: string;
  readonly filters: ProductFilters;
  readonly sort: ProductSort;
  readonly page: number;
  readonly pageSize: number;
}
