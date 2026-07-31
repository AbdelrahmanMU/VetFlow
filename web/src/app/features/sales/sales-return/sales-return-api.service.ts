import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import {
  AddSalesReturnLinePayload,
  CreateSalesReturnPayload,
  ReturnableSaleLine,
} from './sales-return.models';

/**
 * Data access of the sales-return slice (REQ-SAL-004).
 *
 * There is deliberately **no cancel call**: a committed return has no reversal path
 * (DEC-INV-037), and the API exposes none.
 */
@Injectable()
export class SalesReturnApiService {
  private readonly api = inject(ApiClient);

  /** The invoice's lines with what remains returnable. 404 when the invoice is not Committed. */
  getReturnableLines(invoiceId: string): Observable<readonly ReturnableSaleLine[]> {
    return this.api.get<readonly ReturnableSaleLine[]>(`/sales-invoices/${invoiceId}/returnable-lines`);
  }

  createReturn(payload: CreateSalesReturnPayload): Observable<{ readonly id: string; readonly number: string }> {
    return this.api.post<{ readonly id: string; readonly number: string }>('/sales-returns', payload);
  }

  addLine(returnId: string, payload: AddSalesReturnLinePayload): Observable<{ readonly id: string }> {
    return this.api.post<{ readonly id: string }>(`/sales-returns/${returnId}/lines`, payload);
  }

  commit(returnId: string): Observable<void> {
    return this.api.post<void>(`/sales-returns/${returnId}/commit`, {});
  }
}
