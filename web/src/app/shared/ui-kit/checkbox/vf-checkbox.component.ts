import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Labelled checkbox with an always-visible focus ring (design language §14). */
@Component({
  selector: 'vf-checkbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="checkbox">
      <input
        type="checkbox"
        class="checkbox-input"
        [checked]="checked()"
        [disabled]="disabled()"
        (change)="toggled.emit()"
      />
      <span class="checkbox-label"><ng-content /></span>
    </label>
  `,
  styles: `
    .checkbox {
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      cursor: pointer;
      padding-block: var(--vf-space-1);
    }

    .checkbox-input {
      inline-size: 1rem;
      block-size: 1rem;
      accent-color: var(--vf-primary);
      cursor: pointer;
    }

    .checkbox-label {
      font-size: var(--vf-text-body);
      color: var(--vf-text);
    }

    .checkbox:has(.checkbox-input:disabled) {
      opacity: 0.55;
      cursor: default;
    }
  `,
})
export class VfCheckboxComponent {
  readonly checked = input.required<boolean>();
  readonly disabled = input(false);
  readonly toggled = output<void>();
}
