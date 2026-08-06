import { Routes } from '@angular/router';

/** لوحة التشغيل (REQ-DSH-001) — one screen, no children, no parameters (BR-DSH-020). */
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./dashboard-page.component').then((m) => m.DashboardPageComponent),
  },
];
