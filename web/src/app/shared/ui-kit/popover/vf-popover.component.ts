import { ChangeDetectionStrategy, Component, viewChild } from '@angular/core';
import { Popover, PopoverModule } from 'primeng/popover';

/**
 * A small anchored panel for lightweight controls (e.g. column visibility).
 * Focus behavior comes from the underlying overlay.
 */
@Component({
  selector: 'vf-popover',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PopoverModule],
  template: `
    <p-popover #panel styleClass="vf-popover">
      <ng-content />
    </p-popover>
  `,
  styles: `
    ::ng-deep .vf-popover {
      font-family: var(--vf-font);
    }

    ::ng-deep .vf-popover .p-popover-content {
      padding: var(--vf-space-3) var(--vf-space-4);
    }
  `,
})
export class VfPopoverComponent {
  private readonly panel = viewChild.required<Popover>('panel');

  toggle(event: Event): void {
    this.panel().toggle(event);
  }
}
