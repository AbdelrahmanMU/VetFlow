import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { MessageKey } from '../../../core/i18n/ar';
import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { VfSelectComponent } from '../../../shared/ui-kit/select/vf-select.component';
import { AdjustmentApiService } from './adjustment-api.service';
import { AdjustmentStore } from './adjustment.store';
import { ADJUSTMENT_REASONS, AdjustmentDirection, AdjustmentFailure, AdjustmentReason } from './adjustment.models';

const FAILURE_MESSAGES: Readonly<Record<AdjustmentFailure, MessageKey>> = {
  belowZero: 'adjustment.error.belowZero',
  conflict: 'adjustment.error.conflict',
  reason: 'adjustment.error.reason',
  notFound: 'adjustment.error.notFound',
  unknown: 'adjustment.error.unknown',
};

/**
 * تسوية مخزون (inventory ui.md, REQ-INV-010) — the first Inventory screen that *changes* stock.
 *
 * <b>It is a screen of its own, not a button in the batch viewer</b>, because AC-INV-021 and
 * BR-INV-018 forbid any action that edits a quantity inside that viewer. The placement is an
 * implementation choice that keeps an approved rule intact, not a new rule.
 *
 * The direction is chosen explicitly and the amount is always positive: the sign is the server's
 * (BR-INV-064), so a stray minus can never silently invert the operation. The reason list is the
 * adjustment one only — «منتهي الصلاحية» and «ملوَّث» belong to write-off (DEC-INV-031). The actor
 * is an optional free-text field, shown because BR-INV-066 allows it and hiding it would make the
 * rule dead; it is never validated and never required (DEC-INV-030).
 */
@Component({
  selector: 'app-adjustment-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdjustmentApiService, AdjustmentStore],
  imports: [
    FormsModule,
    RouterLink,
    VfSelectComponent,
    VfNumberInputComponent,
    VfTextInputComponent,
    VfTextareaComponent,
    VfButtonComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('adjustment.title') }}</h1>
        <p class="page-subtitle">{{ t.t('adjustment.subtitle') }}</p>
      </header>

      <section class="form-card">
        @if (submitState().kind === 'saved') {
          <p class="banner banner--success" role="status">
            {{ t.t('adjustment.saved') }}
            <a class="history-link" routerLink="/inventory/history">{{ t.t('adjustment.saved.link') }}</a>
          </p>
        }

        @if (failureMessage(); as message) {
          <p class="banner banner--error" role="alert">{{ message }}</p>
        }

        <vf-select
          [label]="t.t('adjustment.field.product')"
          [required]="true"
          [filterable]="true"
          [placeholder]="t.t('adjustment.field.productPlaceholder')"
          [optionList]="productOptions()"
          [value]="productId()"
          (valueChange)="onProductChange($event)"
          [error]="productError()"
        />

        <vf-select
          [label]="t.t('adjustment.field.batch')"
          [required]="true"
          [placeholder]="batchPlaceholder()"
          [optionList]="batchOptions()"
          [value]="batchId()"
          (valueChange)="batchId.set($event)"
          [error]="batchError()"
        />

        <vf-select
          [label]="t.t('adjustment.field.direction')"
          [required]="true"
          [optionList]="directionOptions()"
          [value]="direction()"
          (valueChange)="onDirectionChange($event)"
        />

        <vf-number-input
          [label]="t.t('adjustment.field.quantity')"
          [required]="true"
          [ngModel]="quantity()"
          (ngModelChange)="quantity.set($event)"
          [ngModelOptions]="{ standalone: true }"
          [error]="quantityError()"
        />

        <vf-select
          [label]="t.t('adjustment.field.reason')"
          [required]="true"
          [placeholder]="t.t('adjustment.field.reasonPlaceholder')"
          [optionList]="reasonOptions()"
          [value]="reason()"
          (valueChange)="onReasonChange($event)"
          [error]="reasonError()"
        />

        <vf-textarea
          [label]="t.t('adjustment.field.note')"
          [rows]="3"
          [ngModel]="note()"
          (ngModelChange)="note.set($event)"
          [ngModelOptions]="{ standalone: true }"
        />

        <vf-text-input
          [label]="t.t('adjustment.field.actor')"
          [placeholder]="t.t('adjustment.field.actorPlaceholder')"
          [ngModel]="actor()"
          (ngModelChange)="actor.set($event)"
          [ngModelOptions]="{ standalone: true }"
        />
        <p class="field-hint">{{ t.t('adjustment.field.actorHint') }}</p>

        <div class="actions">
          <vf-button variant="primary" [disabled]="submitState().kind === 'saving'" (pressed)="onSave()">
            {{ t.t('adjustment.action.save') }}
          </vf-button>
        </div>
      </section>
    </div>
  `,
  styles: `
    .page {
      max-inline-size: var(--vf-content-max-width);
      inline-size: 100%;
      margin-inline: auto;
      padding: var(--vf-space-5) var(--vf-space-6);
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
    }

    .page-header {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .page-subtitle,
    .field-hint {
      margin: 0;
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
    }

    .form-card {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
      max-inline-size: 34rem;
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-5);
    }

    .banner {
      margin: 0;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius);
      font-size: var(--vf-text-caption);
    }

    .banner--error {
      background: var(--vf-danger-soft);
      color: var(--vf-danger);
    }

    .banner--success {
      background: var(--vf-success-soft);
      color: var(--vf-success);
    }

    .history-link {
      color: inherit;
      font-weight: 600;
    }

    .actions {
      display: flex;
      justify-content: flex-start;
      margin-block-start: var(--vf-space-2);
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }

      .form-card {
        padding: var(--vf-space-4);
      }
    }
  `,
})
export class AdjustmentPageComponent implements OnInit {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(AdjustmentStore);

  protected readonly productId = signal<string | null>(null);
  protected readonly batchId = signal<string | null>(null);
  protected readonly direction = signal<AdjustmentDirection>('increase');
  protected readonly quantity = signal<number | null>(null);
  protected readonly reason = signal<AdjustmentReason | null>(null);
  protected readonly note = signal('');
  protected readonly actor = signal('');
  protected readonly submitted = signal(false);

  protected readonly submitState = this.store.submit;

  protected readonly productOptions = computed(() =>
    this.store.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly batchOptions = computed(() =>
    this.store.batches().map((batch) => ({
      // The remaining quantity is shown because it is the number the user is correcting; it stays a
      // read-only fact from the batch, never an editable field here.
      label: `${batch.batchId.split('-')[0]} · ${this.format.decimal(batch.remainingQuantity)} ${batch.stockUnitName}`,
      value: batch.batchId,
    })),
  );

  protected readonly directionOptions = computed(() => [
    { label: this.t.t('adjustment.direction.increase'), value: 'increase' },
    { label: this.t.t('adjustment.direction.decrease'), value: 'decrease' },
  ]);

  protected readonly reasonOptions = computed(() =>
    ADJUSTMENT_REASONS.map((reason) => ({
      label: this.t.t(`adjustment.reason.${reason}` as MessageKey),
      value: reason,
    })),
  );

  protected readonly batchPlaceholder = computed(() =>
    this.store.batchesLoading()
      ? this.t.t('adjustment.field.batchLoading')
      : this.productId()
        ? this.t.t('adjustment.field.batchPlaceholder')
        : this.t.t('adjustment.field.batchPickProduct'),
  );

  protected readonly productError = computed(() =>
    this.submitted() && !this.productId() ? this.t.t('adjustment.error.productRequired') : null,
  );

  protected readonly batchError = computed(() =>
    this.submitted() && !this.batchId() ? this.t.t('adjustment.error.batchRequired') : null,
  );

  protected readonly quantityError = computed(() => {
    if (!this.submitted()) {
      return null;
    }

    const quantity = this.quantity();
    return quantity === null || quantity <= 0 ? this.t.t('adjustment.error.quantityPositive') : null;
  });

  protected readonly reasonError = computed(() =>
    this.submitted() && !this.reason() ? this.t.t('adjustment.error.reasonRequired') : null,
  );

  protected readonly failureMessage = computed(() => {
    const state = this.submitState();
    return state.kind === 'failed' ? this.t.t(FAILURE_MESSAGES[state.failure]) : null;
  });

  ngOnInit(): void {
    this.store.loadProducts();
  }

  protected onProductChange(productId: string | null): void {
    this.productId.set(productId);
    this.batchId.set(null);
    this.store.reset();
    this.store.loadBatches(productId);
  }

  protected onDirectionChange(value: string | null): void {
    this.direction.set(value === 'decrease' ? 'decrease' : 'increase');
    this.store.reset();
  }

  protected onReasonChange(value: string | null): void {
    this.reason.set((value as AdjustmentReason | null) ?? null);
    this.store.reset();
  }

  protected onSave(): void {
    this.submitted.set(true);

    const batchId = this.batchId();
    const quantity = this.quantity();
    const reason = this.reason();
    if (!this.productId() || !batchId || quantity === null || quantity <= 0 || !reason) {
      return;
    }

    this.store.save({
      batchId,
      direction: this.direction(),
      quantity,
      reason,
      reasonNote: this.note().trim() || null,
      actorName: this.actor().trim() || null,
    });
  }
}
