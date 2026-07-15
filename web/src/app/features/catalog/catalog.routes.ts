import { Routes } from '@angular/router';

export const CATALOG_ROUTES: Routes = [
  {
    path: 'products',
    loadComponent: () =>
      import('./product-list/product-list-page.component').then((m) => m.ProductListPageComponent),
  },
  {
    path: 'products/new',
    loadComponent: () =>
      import('./product-editor/product-create-page.component').then((m) => m.ProductCreatePageComponent),
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./product-details/product-details-page.component').then((m) => m.ProductDetailsPageComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
];
