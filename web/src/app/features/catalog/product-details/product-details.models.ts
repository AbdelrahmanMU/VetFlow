/** Contract types of GET /api/v1/products/{id} (screen S2, ADR-0015). */

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

export type ProductStatus = 'active' | 'disabled';

export interface ProductUnitDetails {
  readonly unitId: string;
  readonly unitName: string;
  readonly position: number;
  readonly quantityInNextUnit: number | null;
  readonly isPurchaseUnit: boolean;
  readonly isSaleUnit: boolean;
  readonly barcode: string | null;
  readonly sellingPrice: Money | null;
  readonly isStorageUnit: boolean;
  readonly isDefaultSaleUnit: boolean;
  readonly isDefaultPurchaseUnit: boolean;
}

export interface ProductDetails {
  readonly id: string;
  readonly internalCode: string;
  readonly arabicName: string;
  readonly englishName: string | null;
  readonly size: string | null;
  readonly concentration: string | null;
  readonly categoryName: string;
  readonly manufacturerName: string;
  readonly natureName: string;
  readonly status: ProductStatus;
  readonly isSplittable: boolean;
  readonly isRefrigerated: boolean;
  readonly hasExpiration: boolean;
  readonly hasOpenExpiration: boolean;
  readonly openExpirationPeriodDays: number | null;
  readonly internalNotes: string | null;
  readonly hasSellingPrice: boolean;
  readonly units: readonly ProductUnitDetails[];
}
