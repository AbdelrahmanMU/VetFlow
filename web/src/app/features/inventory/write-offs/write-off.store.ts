import { Injectable, inject, signal } from '@angular/core';

import { ApiClient } from '../../../core/api/api-client';
import { ApiError } from '../../../core/api/problem-details';
import { AdjustmentApiService } from '../adjustments/adjustment-api.service';
import { AdjustmentFailure, BatchPickerOption, ProductPickerOption } from '../adjustments/adjustment.models';
import { WriteOffPayload } from './write-off.models';

const BelowZeroCode = 'VTF-INV-061';
const ReasonNotAllowedCode = 'VTF-INV-067';
const ConflictCode = 'VTF-INV-068';

export type WriteOffSubmitState =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved'; readonly movementId: string }
  | { readonly kind: 'failed'; readonly failure: AdjustmentFailure };

/**
 * Write-off state (REQ-INV-011). The product and batch pickers are the <b>same reads</b> the
 * adjustment screen uses, so {@link AdjustmentApiService} is reused rather than copied — the only
 * genuinely different call is the write-off POST itself.
 *
 * Expired batches are offered deliberately: refusing them here would leave the stranded stock R9
 * described with no way out (DEC-INV-021 governs selling, not disposal).
 */
@Injectable()
export class WriteOffStore {
  private readonly api = inject(ApiClient);
  private readonly pickers = inject(AdjustmentApiService);

  readonly products = signal<readonly ProductPickerOption[]>([]);
  readonly batches = signal<readonly BatchPickerOption[]>([]);
  readonly batchesLoading = signal(false);
  readonly submit = signal<WriteOffSubmitState>({ kind: 'idle' });

  loadProducts(): void {
    this.pickers.getActiveProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => this.products.set([]),
    });
  }

  loadBatches(productId: string | null): void {
    this.batches.set([]);
    if (!productId) {
      return;
    }

    this.batchesLoading.set(true);
    this.pickers.getBatches(productId).subscribe({
      next: (batches) => {
        this.batches.set(batches);
        this.batchesLoading.set(false);
      },
      error: () => {
        this.batches.set([]);
        this.batchesLoading.set(false);
      },
    });
  }

  save(payload: WriteOffPayload): void {
    this.submit.set({ kind: 'saving' });
    this.api.post<{ readonly movementId: string }>('/inventory/write-offs', payload).subscribe({
      next: (result) => {
        this.submit.set({ kind: 'saved', movementId: result.movementId });
        this.loadBatches(null);
      },
      error: (error: unknown) => this.submit.set({ kind: 'failed', failure: classify(error) }),
    });
  }

  reset(): void {
    this.submit.set({ kind: 'idle' });
  }
}

function classify(error: unknown): AdjustmentFailure {
  if (!(error instanceof ApiError)) {
    return 'unknown';
  }

  switch (error.errorCode) {
    case BelowZeroCode:
      return 'belowZero';
    case ConflictCode:
      return 'conflict';
    case ReasonNotAllowedCode:
      return 'reason';
    default:
      return error.status === 404 ? 'notFound' : 'unknown';
  }
}
