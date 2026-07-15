import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Empty and error states (STD-FE-030): an invitation, not an apology
 * (design language §6). Actions are projected by the caller.
 */
@Component({
  selector: 'vf-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty" [class.empty--error]="tone() === 'error'" role="status">
      <i class="pi {{ icon() }} empty-icon" aria-hidden="true"></i>
      <p class="empty-title">{{ title() }}</p>
      @if (body(); as bodyText) {
        <p class="empty-body">{{ bodyText }}</p>
      }
      <div class="empty-actions">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: var(--vf-space-7) var(--vf-space-5);
      gap: var(--vf-space-1);
    }

    .empty-icon {
      font-size: 1.75rem;
      color: var(--vf-text-faint);
      margin-block-end: var(--vf-space-3);
    }

    .empty--error .empty-icon {
      color: var(--vf-danger);
    }

    .empty-title {
      margin: 0;
      font-size: var(--vf-text-section-title);
      font-weight: 600;
      color: var(--vf-text);
    }

    .empty-body {
      margin: 0;
      color: var(--vf-text-secondary);
      max-inline-size: 42ch;
    }

    .empty-actions {
      margin-block-start: var(--vf-space-4);
      display: flex;
      gap: var(--vf-space-2);
    }

    .empty-actions:empty {
      display: none;
    }
  `,
})
export class VfEmptyStateComponent {
  readonly icon = input.required<string>();
  readonly title = input.required<string>();
  readonly body = input<string | null>(null);
  readonly tone = input<'neutral' | 'error'>('neutral');
}
