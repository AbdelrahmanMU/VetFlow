import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type VfBannerTone = 'error' | 'success' | 'warning';

/**
 * The single operation-level message surface (validation-and-guidance.md §13
 * item 2, STD-UX-062): tones error/success/warning on the standard tokens,
 * focusable (`tabindex="-1"`) so a rejection with no field target can receive
 * focus itself (STD-UX-071), with the alert/status semantics built in
 * (STD-UX-092 — errors alert, success and warnings announce politely).
 * Screens project the message content; locally re-declared banner markup and
 * CSS are prohibited on compliant screens (STD-UX-121).
 */
@Component({
  selector: 'vf-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ng-content />`,
  host: {
    tabindex: '-1',
    '[attr.role]': 'role()',
    '[class.vf-banner--error]': 'tone() === "error"',
    '[class.vf-banner--success]': 'tone() === "success"',
    '[class.vf-banner--warning]': 'tone() === "warning"',
  },
  styles: `
    :host {
      display: block;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius);
      font-size: var(--vf-text-caption);
      font-weight: 500;
      /* Headroom when the focus service scrolls the banner into view (STD-UX-070/071). */
      scroll-margin-block: var(--vf-space-7);
    }

    :host(.vf-banner--error) {
      background: var(--vf-danger-soft);
      color: var(--vf-danger);
    }

    :host(.vf-banner--success) {
      background: var(--vf-success-soft);
      color: var(--vf-success);
    }

    :host(.vf-banner--warning) {
      background: var(--vf-warning-soft);
      color: var(--vf-warning);
    }
  `,
})
export class VfBannerComponent {
  readonly tone = input.required<VfBannerTone>();

  /** Errors interrupt (`alert`); success and warnings announce politely (`status`) — STD-UX-092. */
  protected readonly role = computed(() => (this.tone() === 'error' ? 'alert' : 'status'));
}
