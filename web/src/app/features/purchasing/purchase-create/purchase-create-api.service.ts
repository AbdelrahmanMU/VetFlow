import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { CreatePurchasePayload, CreatedPurchase } from './purchase-create.models';

/** Data access of the create-purchase slice (REQ-PUR-003) — the header write. */
@Injectable()
export class PurchaseCreateApiService {
  private readonly api = inject(ApiClient);

  create(payload: CreatePurchasePayload): Observable<CreatedPurchase> {
    return this.api.post<CreatedPurchase>('/purchase-invoices', payload);
  }
}
