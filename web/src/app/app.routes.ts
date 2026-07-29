import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalog/products' },
  {
    path: 'catalog',
    loadChildren: () => import('./features/catalog/catalog.routes').then((m) => m.CATALOG_ROUTES),
  },
  {
    path: 'categories',
    loadChildren: () => import('./features/categories/categories.routes').then((m) => m.CATEGORIES_ROUTES),
  },
  {
    path: 'manufacturers',
    loadChildren: () => import('./features/manufacturers/manufacturers.routes').then((m) => m.MANUFACTURERS_ROUTES),
  },
  {
    path: 'purchases',
    loadChildren: () => import('./features/purchasing/purchasing.routes').then((m) => m.PURCHASING_ROUTES),
  },
  {
    path: 'inventory',
    loadChildren: () => import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
  },
  { path: '**', redirectTo: 'catalog/products' },
];
