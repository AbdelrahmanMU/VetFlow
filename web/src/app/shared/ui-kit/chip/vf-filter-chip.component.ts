import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * An applied-filter chip (catalog ui.md S1): the user always sees why the
 * list in front of them is short.
 */
@Component({
  selector: 'vf-filter-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="chip">
      <span class="chip-label">{{ label() }}</span>
      <button type="button" class="chip-remove" [attr.aria-label]="removeLabel()" (click)="removed.emit()">
        <i class="pi pi-times" aria-hidden="true"></i>
      </button>
    </span>
  `,
  styles: `
    .chip {
      display: inline-flex;
      align-items: center;
      gap: var(--vf-space-1);
      background: var(--vf-primary-soft);
      color: var(--vf-primary-strong);
      border: 1px solid color-mix(in srgb, var(--vf-primary) 25%, transparent);
      border-radius: 999px;
      padding-block: 0.125rem;
      padding-inline: 0.625rem 0.25rem;
      font-size: var(--vf-text-secondary-size);
      font-weight: 500;
    }

    .chip-remove {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      inline-size: 1.25rem;
      block-size: 1.25rem;
      border: none;
      border-radius: 999px;
      background: transparent;
      color: inherit;
      cursor: pointer;
    }

    .chip-remove:hover {
      background: color-mix(in srgb, var(--vf-primary) 15%, transparent);
    }

    .chip-remove .pi {
      font-size: 0.625rem;
    }
  `,
})
export class VfFilterChipComponent {
  readonly label = input.required<string>();
  readonly removeLabel = input('');
  readonly removed = output<void>();
}
