import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../core/api/api-client';
import { Dashboard } from './dashboard.models';

/**
 * Data access for the operational dashboard (REQ-DSH-010).
 *
 * **One call, no parameters.** The board is not filtered, sorted, paged or personalised
 * (BR-DSH-020), and it is never fetched per card (BR-DSH-019).
 */
@Injectable()
export class DashboardApiService {
  private readonly api = inject(ApiClient);

  getDashboard(): Observable<Dashboard> {
    return this.api.get<Dashboard>('/dashboard');
  }
}
