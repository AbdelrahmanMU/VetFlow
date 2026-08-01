import { ApiError } from '../api/problem-details';
import { ApiErrorMapper } from './api-error-mapper';
import { VTF_ERROR_REGISTRY } from './validation-registry';

/**
 * The published Error Catalog (ADR-0018): 33 codes. The registry must cover
 * every one (STD-UX-113) — a backend code without a frontend mapping would
 * silently fall to the generic fallback (STD-UX-036).
 */
const CATALOG_CODES = [
  'VTF-VAL-001',
  'VTF-CAT-009',
  'VTF-CAT-016',
  'VTF-CAT-020',
  'VTF-CAT-021',
  'VTF-CAT-022',
  'VTF-CAT-025',
  'VTF-CAT-036',
  'VTF-PUR-003',
  'VTF-PUR-005',
  'VTF-PUR-006',
  'VTF-PUR-007',
  'VTF-PUR-015',
  'VTF-PUR-016',
  'VTF-PUR-017',
  'VTF-PUR-018',
  'VTF-PUR-019',
  'VTF-SAL-003',
  'VTF-SAL-004',
  'VTF-SAL-009',
  'VTF-SAL-012',
  'VTF-SAL-015',
  'VTF-SAL-016',
  'VTF-SAL-017',
  'VTF-SAL-018',
  'VTF-SAL-019',
  'VTF-SAL-020',
  'VTF-INV-046',
  'VTF-INV-052',
  'VTF-INV-056',
  'VTF-INV-061',
  'VTF-INV-067',
  'VTF-INV-068',
];

function apiError(status: number, errorCode?: string, extras?: object): ApiError {
  return new ApiError(status, {
    type: errorCode ? `https://vetflow.app/errors/${errorCode}` : 'about:blank',
    title: 'x',
    status,
    ...(errorCode ? { errorCode } : {}),
    ...extras,
  });
}

describe('VTF_ERROR_REGISTRY', () => {
  it('covers the complete backend Error Catalog, exactly (STD-UX-113)', () => {
    expect(Object.keys(VTF_ERROR_REGISTRY).sort()).toEqual([...CATALOG_CODES].sort());
  });

  it('marks exactly the two concurrency rules retryable (STD-BE-033, DEC-INV-023)', () => {
    const retryable = Object.entries(VTF_ERROR_REGISTRY)
      .filter(([, entry]) => entry.retryable)
      .map(([code]) => code)
      .sort();
    expect(retryable).toEqual(['VTF-INV-056', 'VTF-INV-068']);
  });
});

describe('ApiErrorMapper', () => {
  const mapper = new ApiErrorMapper();

  it('classifies a business code with its registry default (STD-UX-030)', () => {
    const failure = mapper.map(apiError(409, 'VTF-PUR-016'));
    expect(failure.kind).toBe('business');
    expect(failure.messageKey).toBe('errors.VTF-PUR-016');
    expect(failure.retryable).toBe(false);
  });

  it('applies a ruled contextual override (STD-UX-111)', () => {
    const failure = mapper.map(apiError(409, 'VTF-INV-061'), {
      'VTF-INV-061': 'adjustment.error.belowZero',
    });
    expect(failure.messageKey).toBe('adjustment.error.belowZero');
  });

  it('classifies concurrency as retryable (STD-UX-033)', () => {
    expect(mapper.map(apiError(409, 'VTF-INV-068')).retryable).toBe(true);
    expect(mapper.map(apiError(409, 'VTF-INV-068')).kind).toBe('concurrency');
  });

  it('carries the field dictionary of a VTF-VAL-001 response for projection (STD-UX-019)', () => {
    const failure = mapper.map(apiError(400, 'VTF-VAL-001', { errors: { name: ['x'] } }));
    expect(failure.kind).toBe('field');
    expect(failure.fieldErrors).toEqual({ name: ['x'] });
  });

  it('passes metadata through as message params — data, never copy (STD-UX-034)', () => {
    const failure = mapper.map(
      apiError(409, 'VTF-INV-052', { metadata: { products: 'أموكسيسيلين' } }),
    );
    expect(failure.params).toEqual({ products: 'أموكسيسيلين' });
  });

  it('classifies a code-less 404 as notFound — the status branch stands until AMD-1', () => {
    const failure = mapper.map(apiError(404), { notFound: 'adjustment.error.notFound' });
    expect(failure.kind).toBe('notFound');
    expect(failure.messageKey).toBe('adjustment.error.notFound');
  });

  it('classifies anything else — network, 500, unknown code — as system (STD-UX-040/115)', () => {
    expect(mapper.map(new Error('offline')).kind).toBe('system');
    expect(mapper.map(apiError(500)).kind).toBe('system');
    expect(mapper.map(apiError(409, 'VTF-XXX-999')).kind).toBe('system');
    expect(
      mapper.map(new Error('offline'), { system: 'adjustment.error.unknown' }).messageKey,
    ).toBe('adjustment.error.unknown');
  });
});
