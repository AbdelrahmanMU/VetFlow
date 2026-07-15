import { Routes } from '@angular/router';

export const CATALOG_ROUTES: Routes = [
  {
    path: 'products',
    loadComponent: () =>
      import('./product-list/product-list-page.component').then((m) => m.ProductListPageComponent),
  },
  {
    path: 'products/new',
    data: { mode: 'create' },
    loadComponent: () =>
      import('./product-editor/product-editor-page.component').then((m) => m.ProductEditorPageComponent),
  },
  {
    path: 'products/:id/edit',
    data: { mode: 'edit' },
    loadComponent: () =>
      import('./product-editor/product-editor-page.component').then((m) => m.ProductEditorPageComponent),
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./product-details/product-details-page.component').then((m) => m.ProductDetailsPageComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'products' },
];
