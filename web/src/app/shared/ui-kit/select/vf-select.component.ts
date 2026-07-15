import { ChangeDetectionStrategy, Component, computed, effect, input, model } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SelectModule } from 'primeng/select';

export interface VfSelectOption<T> {
  readonly label: string;
  readonly value: T;
}

/**
 * Searchable single select (catalog ui.md §12) over a typed reactive control
 * (STD-FE-016). Wraps the component foundation; features never import it
 * directly (ADR-0012).
 */
@Component({
  selector: 'vf-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule],
  template: `
    <div class="select-label">
      <span class="select-caption">{{ label() }}</span>
      <p-select
        [formControl]="control"
        [options]="mutableOptions()"
        optionLabel="label"
        optionValue="value"
        [filter]="filterable()"
        [showClear]="clearable()"
        [placeholder]="placeholder()"
        appendTo="body"
        styleClass="vf-select"
        [ariaLabel]="label()"
      />
    </div>
  `,
  styles: `
    .select-label {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .select-caption {
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    ::ng-deep .vf-select {
      inline-size: 100%;
      font-family: var(--vf-font);
    }
  `,
})
export class VfSelectComponent<T> {
  readonly label = input('');
  readonly placeholder = input('');
  readonly filterable = input(false);
  readonly clearable = input(true);
  readonly optionList = input.required<readonly VfSelectOption<T>[]>();
  readonly value = model<T | null>(null);

  protected readonly control = new FormControl<T | null>(null);

  // The underlying select expects a mutable array; the input stays readonly.
  protected readonly mutableOptions = computed(() => [...this.optionList()]);

  constructor() {
    effect(() => {
      const next = this.value();
      if (this.control.value !== next) {
        this.control.setValue(next, { emitEvent: false });
      }
    });

    this.control.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((next) => this.value.set(next ?? null));
  }
}
