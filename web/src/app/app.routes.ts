import { Routes } from '@angular/router';

import { anonymousOnlyGuard, authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './core/layout/shell.component';

/**
 * Two branches: the login screen, which stands alone because there is no navigation to offer
 * someone who is not signed in — and everything else, inside the shell and behind the guard.
 *
 * <b>The landing route is the operational dashboard</b> (REQ-DSH-001, DEC-DSH-011, owner ruling
 * 2026-08-03). It supersedes DEC-IDN-007 («the first screen is the product list, and no dashboard
 * is built»), whose own stated basis — the inventory scope-lock — the owner lifted when they
 * commissioned the board. The identifier is preserved, not reused; the supersession is recorded in
 * `docs/modules/identity/decisions.md`.
 *
 * Nothing else about sign-in changed: the dashboard sits behind the same guard as every other
 * business screen (REQ-IDN-006, BR-IDN-005).
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
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
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
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
  // Last, so it can never swallow `/login`: an unknown path visited without a session falls past
  // the guarded branch above and lands here.
  { path: '**', redirectTo: 'login' },
];
