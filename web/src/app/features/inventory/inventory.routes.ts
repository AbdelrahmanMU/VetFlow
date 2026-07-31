import { Routes } from '@angular/router';

export const INVENTORY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./inventory-list/inventory-list-page.component').then((m) => m.InventoryListPageComponent),
  },
  // Static segments must precede the :productId batch viewer so they are not swallowed by the
  // wildcard (the /purchases/new-before-:id precedent).
  {
    path: 'expiry',
    loadComponent: () =>
      import('./expiry-monitoring/expiry-monitoring-page.component').then(
        (m) => m.ExpiryMonitoringPageComponent,
      ),
  },
  {
    path: 'adjustments/new',
    loadComponent: () =>
      import('./adjustments/adjustment-page.component').then((m) => m.AdjustmentPageComponent),
  },
  {
    path: 'write-offs/new',
    loadComponent: () =>
      import('./write-offs/write-off-page.component').then((m) => m.WriteOffPageComponent),
  },
  {
    path: 'history',
    loadComponent: () =>
      import('./movement-history/movement-history-page.component').then(
        (m) => m.MovementHistoryPageComponent,
      ),
  },
  {
    path: ':productId',
    loadComponent: () =>
      import('./batch-viewer/batch-viewer-page.component').then((m) => m.BatchViewerPageComponent),
  },
];
