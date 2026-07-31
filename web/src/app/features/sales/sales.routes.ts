import { Routes } from '@angular/router';

/**
 * Sales routes (sales ui.md). There is no list route: a sales list is not one of the five slices
 * and was not invented (DEC-SAL-005 — open). `new` is registered before `:id` so the literal
 * segment wins — the same order as purchasing and inventory.
 */
export const SALES_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'new' },
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
