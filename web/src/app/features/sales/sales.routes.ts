import { Routes } from '@angular/router';

/**
 * Sales routes (sales ui.md). The list is the entry (REQ-SAL-005, DEC-SAL-005 —
 * owner-ruled 2026-07-31). `new` is registered before `:id` so the literal
 * segment wins — the same order as purchasing and inventory.
 */
export const SALES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./sales-list/sales-list-page.component').then((m) => m.SalesListPageComponent),
  },
  {
    path: 'new',
    loadComponent: () => import('./sale-create/sale-create-page.component').then((m) => m.SaleCreatePageComponent),
  },
  {
    // Registered before ':id' so the more specific route wins (REQ-SAL-004, sales/ui.md).
    path: ':id/returns/new',
    loadComponent: () =>
      import('./sales-return/sales-return-page.component').then((m) => m.SalesReturnPageComponent),
  },
  {
    path: ':id',
    loadComponent: () => import('./sale-details/sale-details-page.component').then((m) => m.SaleDetailsPageComponent),
  },
];
