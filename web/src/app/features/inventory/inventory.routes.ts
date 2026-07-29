import { Routes } from '@angular/router';

export const INVENTORY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./inventory-list/inventory-list-page.component').then((m) => m.InventoryListPageComponent),
  },
];
