import { Routes } from '@angular/router';

import { anonymousOnlyGuard, authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './core/layout/shell.component';

/**
 * Two branches: the login screen, which stands alone because there is no navigation to offer
 * someone who is not signed in — and everything else, inside the shell and behind the guard.
 *
 * <b>The landing route is unchanged</b>: `/` still goes to the product list (DEC-IDN-007 — «lands
 * in the app» means the screen that already exists; no dashboard is built). It is simply preceded
 * by a sign-in now (REQ-IDN-006, BR-IDN-005).
 */
export const routes: Routes = [
  {
    path: 'login',
    canMatch: [anonymousOnlyGuard],
    loadComponent: () =>
      import('./features/identity/login/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canMatch: [authGuard],
    children: [
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
        loadChildren: () =>
          import('./features/manufacturers/manufacturers.routes').then((m) => m.MANUFACTURERS_ROUTES),
      },
      {
        path: 'purchases',
        loadChildren: () => import('./features/purchasing/purchasing.routes').then((m) => m.PURCHASING_ROUTES),
      },
      {
        path: 'sales',
        loadChildren: () => import('./features/sales/sales.routes').then((m) => m.SALES_ROUTES),
      },
      {
        path: 'inventory',
        loadChildren: () => import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
      },
      { path: '**', redirectTo: 'catalog/products' },
    ],
  },
  // Last, so it can never swallow `/login`: an unknown path visited without a session falls past
  // the guarded branch above and lands here.
  { path: '**', redirectTo: 'login' },
];
