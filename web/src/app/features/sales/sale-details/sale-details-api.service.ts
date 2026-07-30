import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { SaleDetails } from './sale-details.models';

/** Data access of the sale-details slice (REQ-SAL-002). */
@Injectable()
export class SaleDetailsApiService {
  private readonly api = inject(ApiClient);

  getById(id: string): Observable<SaleDetails> {
    return this.api.get<SaleDetails>(`/sales-invoices/${id}`);
  }
}
