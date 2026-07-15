import { Routes } from '@angular/router';

export const CATALOG_ROUTES: Routes = [
  {
    path: 'products',
    loadComponent: () =>
      import('./product-list/product-list-page.component').then((m) => m.ProductListPageComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
];
