import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { PurchaseDetails } from './purchase-details.models';

/** Data access of the purchase-details slice (REQ-PUR-002). */
@Injectable()
export class PurchaseDetailsApiService {
  private readonly api = inject(ApiClient);

  getById(id: string): Observable<PurchaseDetails> {
    return this.api.get<PurchaseDetails>(`/purchase-invoices/${id}`);
  }
}
