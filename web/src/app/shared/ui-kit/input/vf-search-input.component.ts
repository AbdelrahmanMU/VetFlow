import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  input,
  output,
  viewChild,
} from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

/**
 * The hero search field (catalog ui.md S1): wide, auto-focused on open, ready
 * for barcode-wedge scanners, debounced at the stream boundary (STD-FE-013).
 * Esc clears (design language §6 keyboard set).
 */
@Component({
  selector: 'vf-search-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="search">
      <i class="pi pi-search search-icon" aria-hidden="true"></i>
      <input
        #field
        type="search"
        class="search-field"
        [placeholder]="placeholder()"
        [attr.aria-label]="placeholder()"
        [value]="value()"
        (input)="onInput(field.value)"
        (keydown.escape)="clear(field)"
      />
      @if (field.value) {
        <button type="button" class="clear" [attr.aria-label]="clearLabel()" (click)="clear(field)">
          <i class="pi pi-times" aria-hidden="true"></i>
        </button>
      }
    </div>
  `,
  styles: `
    .search {
      position: relative;
      display: flex;
      align-items: center;
      inline-size: 100%;
    }

    .search-icon {
      position: absolute;
      inset-inline-start: var(--vf-space-3);
      color: var(--vf-text-faint);
      font-size: 0.9375rem;
      pointer-events: none;
    }

    .search-field {
      inline-size: 100%;
      font-family: inherit;
      font-size: var(--vf-text-body);
      color: var(--vf-text);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border-strong);
      border-radius: var(--vf-radius-small);
      padding-block: 0.5rem;
      padding-inline: 2.375rem;
      transition: border-color 120ms ease, box-shadow 120ms ease;
    }

    .search-field::placeholder {
      color: var(--vf-text-faint);
    }

    .search-field:focus-visible {
      border-color: var(--vf-primary);
      box-shadow: var(--vf-focus-ring);
      outline: none;
    }

    .search-field::-webkit-search-cancel-button {
      display: none;
    }

    .clear {
      position: absolute;
      inset-inline-end: var(--vf-space-2);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      inline-size: 1.75rem;
      block-size: 1.75rem;
      border: none;
      background: transparent;
      color: var(--vf-text-faint);
      border-radius: var(--vf-radius-small);
      cursor: pointer;
    }

    .clear:hover {
      color: var(--vf-text);
      background: var(--vf-bg);
    }
  `,
})
export class VfSearchInputComponent {
  readonly placeholder = input('');
  readonly clearLabel = input('');
  readonly value = input('');
  readonly autofocus = input(false);
  readonly debouncedValue = output<string>();

  private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');
  private readonly input$ = new Subject<string>();

  constructor() {
    this.input$
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => this.debouncedValue.emit(value));

    afterNextRender(() => {
      if (this.autofocus()) {
        this.field().nativeElement.focus();
      }
    });
  }

  protected onInput(value: string): void {
    this.input$.next(value);
  }

  protected clear(field: HTMLInputElement): void {
    field.value = '';
    this.input$.next('');
    field.focus();
  }
}
