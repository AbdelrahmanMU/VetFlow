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
      [attr.type]="type()"
      class="vf-button"
      [class.vf-button--full]="full()"
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

    /* A single full-width action, as the login screen rules (identity/ui.md S1).
       It carries the same 44 px minimum the compact tier's navigation holds itself
       to (design language §5 amendment / §14: «أهداف لمس مريحة»), because on a phone
       this is the only thing on the screen to press. */
    .vf-button--full {
      inline-size: 100%;
      justify-content: center;
      min-block-size: 2.75rem;
    }

    /* On the touch tiers every button clears 44 px, not only the full-width one
       (design language §14: «أهداف لمس مريحة على اللوحي والجوال»). The padding above
       leaves an ordinary button at ~39 px, which is comfortable with a mouse and short
       with a thumb — and the browser run caught exactly that on the dashboard's primary
       action. Desktop is deliberately untouched: the same breakpoint the shell uses for
       its compact tier (768 px). */
    @media (width <= 768px) {
      .vf-button {
        min-block-size: 2.75rem;
      }
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
  /** Stretches to the container's width — the login screen's single action (identity/ui.md S1). */
  readonly full = input(false);
  /** `submit` participates in `form[vfSubmitGuide]` submission (STD-UX-122). */
  readonly type = input<'button' | 'submit'>('button');
  readonly pressed = output<MouseEvent>();
}
