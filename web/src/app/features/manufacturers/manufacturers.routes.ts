import { Routes } from '@angular/router';

export const MANUFACTURERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./manufacturer-list/manufacturer-list-page.component').then((m) => m.ManufacturerListPageComponent),
  },
];
