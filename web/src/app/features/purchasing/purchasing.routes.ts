import { Routes } from '@angular/router';

export const PURCHASING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./purchase-list/purchase-list-page.component').then((m) => m.PurchaseListPageComponent),
  },
  {
    // Registered before ':id' so the literal segment wins (ui.md, REQ-PUR-003).
    path: 'new',
    loadComponent: () =>
      import('./purchase-create/purchase-create-page.component').then((m) => m.PurchaseCreatePageComponent),
  },
  {
    // Registered before ':id' so the more specific route wins (REQ-PUR-006, purchasing/ui.md).
    path: ':id/returns/new',
    loadComponent: () =>
      import('./purchase-return/purchase-return-page.component').then((m) => m.PurchaseReturnPageComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./purchase-details/purchase-details-page.component').then((m) => m.PurchaseDetailsPageComponent),
  },
];
