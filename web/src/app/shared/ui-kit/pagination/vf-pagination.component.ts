import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';

/**
 * Quiet pagination under the table with a clear result counter
 * (catalog ui.md S1, design language §6). Western digits (ADR-0007).
 */
@Component({
  selector: 'vf-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="pagination" [attr.aria-label]="t.t('pagination.label')">
      <span class="counter vf-num">{{ counter() }}</span>
      <div class="controls">
        <button
          type="button"
          class="page-button"
          [disabled]="page() <= 1"
          (click)="pageChange.emit(page() - 1)"
        >
          {{ t.t('pagination.previous') }}
        </button>
        <span class="page-indicator vf-num">{{ format.integer(page()) }} / {{ format.integer(totalPages()) }}</span>
        <button
          type="button"
          class="page-button"
          [disabled]="page() >= totalPages()"
          (click)="pageChange.emit(page() + 1)"
        >
          {{ t.t('pagination.next') }}
        </button>
      </div>
    </nav>
  `,
  styles: `
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-4);
      padding: var(--vf-space-3) var(--vf-space-4);
    }

    .counter {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
    }

    .controls {
      display: flex;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .page-indicator {
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
    }

    .page-button {
      font-family: inherit;
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border-strong);
      border-radius: var(--vf-radius-small);
      padding: 0.25rem 0.75rem;
      cursor: pointer;
    }

    .page-button:not(:disabled):hover {
      background: var(--vf-bg);
    }

    .page-button:disabled {
      opacity: 0.45;
      cursor: default;
    }
  `,
})
export class VfPaginationComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);

  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );

  protected readonly counter = computed(() => {
    const total = this.totalCount();
    if (total === 0) {
      return this.t.t('pagination.zero');
    }

    const from = (this.page() - 1) * this.pageSize() + 1;
    const to = Math.min(this.page() * this.pageSize(), total);
    return this.t.t('pagination.range', {
      from: this.format.integer(from),
      to: this.format.integer(to),
      total: this.format.integer(total),
    });
  });
}
