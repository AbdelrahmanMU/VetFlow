import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { InventoryListPageComponent } from './inventory-list-page.component';

/**
 * Inventory projection page (REQ-INV-002). Read-only: a row navigates to the future
 * Batch Viewer (BR-INV-015, DEC-INV-007) — this wires the navigation intent only; the
 * viewer is out of scope this slice.
 */
describe('InventoryListPageComponent', () => {
  it('navigates to the future Batch Viewer for the product (BR-INV-015)', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(InventoryListPageComponent);
    const http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    // The store fires the category lookup on construction — answer it.
    http
      .expectOne((candidate) => candidate.url === '/api/v1/categories')
      .flush({ items: [], page: 1, pageSize: 100, totalCount: 0 });

    (fixture.componentInstance as unknown as { goToBatchViewer(productId: string): void }).goToBatchViewer('prod-9');

    expect(navigate).toHaveBeenCalledWith(['/inventory', 'prod-9']);

    // Drain any projection request the view issued so verify() stays clean.
    http
      .match((candidate) => candidate.url === '/api/v1/inventory')
      .forEach((request) => request.flush({ items: [], page: 1, pageSize: 25, totalCount: 0 }));
    http.verify();
  });
});
