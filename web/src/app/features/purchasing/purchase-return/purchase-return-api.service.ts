import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import {
  AddReturnLinePayload,
  CreatePurchaseReturnPayload,
  ReturnableLine,
} from './purchase-return.models';

/**
 * Data access of the purchase-return slice (REQ-PUR-006).
 *
 * There is deliberately **no cancel call**: a committed return has no reversal path
 * (DEC-INV-037), and the API exposes none.
 */
@Injectable()
export class PurchaseReturnApiService {
  private readonly api = inject(ApiClient);

  /** The invoice's lines with what remains returnable. 404 when the invoice is not Received. */
  getReturnableLines(invoiceId: string): Observable<readonly ReturnableLine[]> {
    return this.api.get<readonly ReturnableLine[]>(`/purchase-invoices/${invoiceId}/returnable-lines`);
  }

  createReturn(payload: CreatePurchaseReturnPayload): Observable<{ readonly id: string; readonly number: string }> {
    return this.api.post<{ readonly id: string; readonly number: string }>('/purchase-returns', payload);
  }

  addLine(returnId: string, payload: AddReturnLinePayload): Observable<{ readonly id: string }> {
    return this.api.post<{ readonly id: string }>(`/purchase-returns/${returnId}/lines`, payload);
  }

  commit(returnId: string): Observable<void> {
    return this.api.post<void>(`/purchase-returns/${returnId}/commit`, {});
  }
}
