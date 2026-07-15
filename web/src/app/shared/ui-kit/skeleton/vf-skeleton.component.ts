import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Loading skeleton (STD-FE-031): initial loads render content-shaped
 * skeletons — never spinners. The shimmer is quiet (design language §13).
 */
@Component({
  selector: 'vf-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<span class="skeleton" [style.inline-size]="width()" [style.block-size]="height()"></span>',
  styles: `
    .skeleton {
      display: inline-block;
      background: linear-gradient(90deg, var(--vf-border) 25%, #f0efee 50%, var(--vf-border) 75%);
      background-size: 200% 100%;
      border-radius: var(--vf-radius-small);
      animation: vf-shimmer 1.6s ease-in-out infinite;
    }

    @keyframes vf-shimmer {
      from {
        background-position: 200% 0;
      }

      to {
        background-position: -200% 0;
      }
    }
  `,
})
export class VfSkeletonComponent {
  readonly width = input('100%');
  readonly height = input('1rem');
}
