import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Status badge: always text + color + icon — color never carries the meaning
 * alone (design language §6, §11).
 */
@Component({
  selector: 'vf-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="badge"
      [class.badge--success]="tone() === 'success'"
      [class.badge--warning]="tone() === 'warning'"
      [class.badge--danger]="tone() === 'danger'"
      [class.badge--neutral]="tone() === 'neutral'"
    >
      <i class="pi {{ icon() }}" aria-hidden="true"></i>
      <ng-content />
    </span>
  `,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.3125rem;
      font-size: var(--vf-text-caption);
      font-weight: 600;
      line-height: 1.4;
      padding: 0.125rem 0.5rem;
      border-radius: 999px;
      white-space: nowrap;
    }

    .pi {
      font-size: 0.625rem;
    }

    .badge--success {
      color: var(--vf-success);
      background: var(--vf-success-soft);
    }

    .badge--warning {
      color: var(--vf-warning);
      background: var(--vf-warning-soft);
    }

    .badge--danger {
      color: var(--vf-danger);
      background: var(--vf-danger-soft);
    }

    .badge--neutral {
      color: var(--vf-text-secondary);
      background: var(--vf-bg);
      border: 1px solid var(--vf-border);
    }
  `,
})
export class VfBadgeComponent {
  readonly tone = input<'success' | 'warning' | 'danger' | 'neutral'>('neutral');
  readonly icon = input.required<string>();
}
