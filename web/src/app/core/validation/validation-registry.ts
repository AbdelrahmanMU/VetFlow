import { MessageKey } from '../i18n/ar';

/**
 * The ValidationRegistry (validation-and-guidance.md §12, STD-UX-110): one
 * frontend registry mapping every backend error code to its category, its
 * default message key, and whether it is retryable. Per-store `classify()`
 * copies are prohibited (AP-10) — screens narrow this registry with ruled
 * contextual overrides (STD-UX-111), never fork it.
 *
 * Default wording under `errors.<code>` mirrors the backend Error Catalog
 * resources verbatim (ADR-0018; the two authored copies stay in sync per
 * STD-UX-112). Completeness against the backend catalog is pinned by test
 * (STD-UX-113).
 */
export type RegistryFailureKind = 'field' | 'business' | 'concurrency';

export interface ValidationRegistryEntry {
  readonly kind: RegistryFailureKind;
  readonly messageKey: MessageKey;
  /** Concurrency conflicts always offer retry (STD-UX-033, DEC-INV-023). */
  readonly retryable: boolean;
}

function field(messageKey: MessageKey): ValidationRegistryEntry {
  return { kind: 'field', messageKey, retryable: false };
}

function business(messageKey: MessageKey): ValidationRegistryEntry {
  return { kind: 'business', messageKey, retryable: false };
}

function concurrency(messageKey: MessageKey): ValidationRegistryEntry {
  return { kind: 'concurrency', messageKey, retryable: true };
}

export const VTF_ERROR_REGISTRY: Readonly<Record<string, ValidationRegistryEntry>> = {
  // Common (ADR-0018 §4; STD-API-014 field shape)
  'VTF-VAL-001': field('errors.VTF-VAL-001'),

  // Catalog (BR-CAT-009/016/020/021/022/025/036)
  'VTF-CAT-009': business('errors.VTF-CAT-009'),
  'VTF-CAT-016': business('errors.VTF-CAT-016'),
  'VTF-CAT-020': business('errors.VTF-CAT-020'),
  'VTF-CAT-021': business('errors.VTF-CAT-021'),
  'VTF-CAT-022': business('errors.VTF-CAT-022'),
  'VTF-CAT-025': business('errors.VTF-CAT-025'),
  'VTF-CAT-036': business('errors.VTF-CAT-036'),

  // Purchasing (BR-PUR-003..018)
  'VTF-PUR-003': business('errors.VTF-PUR-003'),
  'VTF-PUR-005': business('errors.VTF-PUR-005'),
  'VTF-PUR-006': business('errors.VTF-PUR-006'),
  'VTF-PUR-007': business('errors.VTF-PUR-007'),
  'VTF-PUR-015': business('errors.VTF-PUR-015'),
  'VTF-PUR-016': business('errors.VTF-PUR-016'),
  'VTF-PUR-017': business('errors.VTF-PUR-017'),
  'VTF-PUR-018': business('errors.VTF-PUR-018'),
  'VTF-PUR-019': business('errors.VTF-PUR-019'),

  // Sales (BR-SAL-003..018)
  'VTF-SAL-003': business('errors.VTF-SAL-003'),
  'VTF-SAL-004': business('errors.VTF-SAL-004'),
  'VTF-SAL-009': business('errors.VTF-SAL-009'),
  'VTF-SAL-012': business('errors.VTF-SAL-012'),
  'VTF-SAL-015': business('errors.VTF-SAL-015'),
  'VTF-SAL-016': business('errors.VTF-SAL-016'),
  'VTF-SAL-017': business('errors.VTF-SAL-017'),
  'VTF-SAL-018': business('errors.VTF-SAL-018'),
  'VTF-SAL-019': business('errors.VTF-SAL-019'),
  'VTF-SAL-020': business('errors.VTF-SAL-020'),

  // Inventory (BR-INV-046..068; 056/068 are the two concurrency rules — one code per rule, STD-BE-033)
  'VTF-INV-046': business('errors.VTF-INV-046'),
  'VTF-INV-052': business('errors.VTF-INV-052'),
  'VTF-INV-056': concurrency('errors.VTF-INV-056'),
  'VTF-INV-061': business('errors.VTF-INV-061'),
  'VTF-INV-067': business('errors.VTF-INV-067'),
  'VTF-INV-068': concurrency('errors.VTF-INV-068'),

  // Identity (BR-IDN-003/008). These two point at the approved identity keys instead of the
  // usual `errors.<code>` ones because identity/ui.md already rules this exact wording, and the
  // backend resources carry the same two sentences verbatim (STD-UX-112). A second copy under a
  // second key would be the same approved sentence twice, free to drift.
  'VTF-IDN-001': business('login.error.invalid'),
  'VTF-IDN-002': business('session.expired'),
};
