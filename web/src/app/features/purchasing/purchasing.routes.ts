import { Routes } from '@angular/router';

export const PURCHASING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./purchase-list/purchase-list-page.component').then((m) => m.PurchaseListPageComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./purchase-details/purchase-details-page.component').then((m) => m.PurchaseDetailsPageComponent),
  },
];
