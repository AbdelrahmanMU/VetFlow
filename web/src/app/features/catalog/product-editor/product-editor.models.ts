/** Contract types of POST /api/v1/products and the possible-duplicate read (ADR-0015). */

export interface LookupOption {
  readonly id: string;
  readonly name: string;
}

/** A category lookup option — carries the active flag so the editor can show only
 *  active values for a new product yet keep a product's current inactive value
 *  visible and selected in edit mode (REQ-CTG-005 / DEC-CTG-002). */
export interface CategoryOption extends LookupOption {
  readonly isActive: boolean;
}

/** A manufacturer lookup option — carries the active flag for the same active-only +
 *  current-inactive editor behavior as categories (REQ-CAT-048 / DEC-CAT-032). */
export interface ManufacturerOption extends LookupOption {
  readonly isActive: boolean;
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

/** One unit-profile row in an update body: no selling price — price editing is the
 *  deferred audited path (DEC-CAT-031), so PUT never carries it. */
export interface UpdateProductUnitRow {
  readonly unitId: string;
  readonly position: number;
  readonly quantityInNextUnit: number | null;
  readonly isPurchaseUnit: boolean;
  readonly isSaleUnit: boolean;
  readonly barcode: string | null;
}

/** Contract of PUT /api/v1/products/{id} — the non-audited unified edit (DEC-CAT-031). */
export interface UpdateProductPayload {
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
  readonly units: readonly UpdateProductUnitRow[];
  readonly storageUnitId: string;
  readonly defaultSaleUnitId: string;
  readonly defaultPurchaseUnitId: string;
}

export interface EditProductMoney {
  readonly amount: number;
  readonly currency: string;
}

/** One unit of a product loaded for editing. The distinguished-unit flags drive
 *  the storage/default-role select prefill (edit mode, DEC-CAT-031). */
export interface EditProductUnit {
  readonly unitId: string;
  readonly position: number;
  readonly quantityInNextUnit: number | null;
  readonly isPurchaseUnit: boolean;
  readonly isSaleUnit: boolean;
  readonly barcode: string | null;
  readonly sellingPrice: EditProductMoney | null;
  readonly isStorageUnit: boolean;
  readonly isDefaultSaleUnit: boolean;
  readonly isDefaultPurchaseUnit: boolean;
}

/** Edit projection of GET /api/v1/products/{id}: only the fields the editor
 *  prefills. The classification ids ride on the details DTO precisely so the
 *  editor can prefill its selects (ProductDetailsDto, DEC-CAT-031). */
export interface EditProduct {
  readonly id: string;
  readonly internalCode: string;
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
  readonly units: readonly EditProductUnit[];
}
