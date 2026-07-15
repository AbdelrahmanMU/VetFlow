import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { DrawerModule } from 'primeng/drawer';

/**
 * Side panel (design language §8): the default editing/filter surface on
 * desktop — the list stays visible behind it. Slides from the inline-end
 * (left in RTL). Focus is trapped and returned by the underlying overlay.
 */
@Component({
  selector: 'vf-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DrawerModule],
  template: `
    <p-drawer
      [(visible)]="visible"
      position="left"
      [header]="header()"
      [modal]="true"
      [blockScroll]="true"
      styleClass="vf-drawer"
    >
      <ng-content />
    </p-drawer>
  `,
  styles: `
    ::ng-deep .vf-drawer {
      inline-size: 22.5rem;
      max-inline-size: 92vw;
      font-family: var(--vf-font);
    }

    ::ng-deep .vf-drawer .p-drawer-header {
      padding: var(--vf-space-4) var(--vf-space-5);
      font-weight: 600;
    }

    ::ng-deep .vf-drawer .p-drawer-content {
      padding: var(--vf-space-2) var(--vf-space-5) var(--vf-space-5);
    }
  `,
})
export class VfDrawerComponent {
  readonly header = input('');
  readonly visible = model(false);
}
