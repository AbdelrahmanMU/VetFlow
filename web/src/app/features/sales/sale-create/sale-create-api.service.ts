import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/api/api-client';
import { CreateSalePayload, CreatedSale } from './sale-create.models';

/** Data access of the create-sale slice (REQ-SAL-001) — the header write. */
@Injectable()
export class SaleCreateApiService {
  private readonly api = inject(ApiClient);

  create(payload: CreateSalePayload): Observable<CreatedSale> {
    return this.api.post<CreatedSale>('/sales-invoices', payload);
  }
}
