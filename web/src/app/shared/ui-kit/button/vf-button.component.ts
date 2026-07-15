import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * VetFlow button. One primary action per screen (design language §15.1);
 * everything else is secondary or quiet.
 */
@Component({
  selector: 'vf-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="vf-button"
      [class.vf-button--primary]="variant() === 'primary'"
      [class.vf-button--secondary]="variant() === 'secondary'"
      [class.vf-button--quiet]="variant() === 'quiet'"
      [disabled]="disabled()"
      (click)="pressed.emit($event)"
    >
      @if (icon(); as iconName) {
        <i class="pi {{ iconName }}" aria-hidden="true"></i>
      }
      <ng-content />
    </button>
  `,
  styles: `
    .vf-button {
      display: inline-flex;
      align-items: center;
      gap: var(--vf-space-2);
      font-family: inherit;
      font-size: var(--vf-text-body);
      font-weight: 500;
      line-height: 1.5;
      padding: 0.4375rem var(--vf-space-4);
      border-radius: var(--vf-radius-small);
      border: 1px solid transparent;
      cursor: pointer;
      transition: background-color 120ms ease, border-color 120ms ease;
    }

    .vf-button:disabled {
      opacity: 0.55;
      cursor: default;
    }

    .vf-button--primary {
      background: var(--vf-primary);
      color: #fff;
    }

    .vf-button--primary:not(:disabled):hover {
      background: var(--vf-primary-strong);
    }

    .vf-button--secondary {
      background: var(--vf-surface);
      color: var(--vf-text);
      border-color: var(--vf-border-strong);
    }

    .vf-button--secondary:not(:disabled):hover {
      background: var(--vf-bg);
    }

    .vf-button--quiet {
      background: transparent;
      color: var(--vf-text-secondary);
    }

    .vf-button--quiet:not(:disabled):hover {
      background: var(--vf-bg);
      color: var(--vf-text);
    }

    .pi {
      font-size: 0.875rem;
    }
  `,
})
export class VfButtonComponent {
  readonly variant = input<'primary' | 'secondary' | 'quiet'>('secondary');
  readonly icon = input<string | null>(null);
  readonly disabled = input(false);
  readonly pressed = output<MouseEvent>();
}
