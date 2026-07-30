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
    path: ':productId',
    loadComponent: () =>
      import('./batch-viewer/batch-viewer-page.component').then((m) => m.BatchViewerPageComponent),
  },
];
