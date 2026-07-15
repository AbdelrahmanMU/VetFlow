/** Contract types of POST /api/v1/products and the possible-duplicate read (ADR-0015). */

export interface LookupOption {
  readonly id: string;
  readonly name: string;
}

export interface CreateProductUnitRow {
  readonly unitId: string;
  readonly position: number;
  readonly quantityInNextUnit: number | null;
  readonly isPurchaseUnit: boolean;
  readonly isSaleUnit: boolean;
  readonly barcode: string | null;
  readonly sellingPrice: number | null;
}

export interface CreateProductPayload {
  readonly arabicName: string;
  readonly englishName: string | null;
  readonly size: string | null;
  readonly concentration: string | null;
  readonly categoryId: string;
  readonly manufacturerId: string;
  readonly natureId: string;
  readonly isSplittable: boolean;
  readonly isRefrigerated: boolean;
  readonly hasExpiration: boolean;
  readonly hasOpenExpiration: boolean;
  readonly openExpirationPeriodDays: number | null;
  readonly internalNotes: string | null;
  readonly units: readonly CreateProductUnitRow[];
  readonly storageUnitId: string;
  readonly defaultSaleUnitId: string;
  readonly defaultPurchaseUnitId: string;
}

export interface CreatedProduct {
  readonly id: string;
  readonly internalCode: string;
}

/** A product flagged as a possible duplicate (DEC-CAT-027); advisory only (BR-CAT-042). */
export interface PossibleDuplicate {
  readonly id: string;
  readonly arabicName: string;
  readonly englishName: string | null;
  readonly size: string | null;
  readonly concentration: string | null;
  readonly manufacturerName: string;
}
