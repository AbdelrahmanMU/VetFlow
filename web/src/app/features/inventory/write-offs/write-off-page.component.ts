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
import { AdjustmentApiService } from '../adjustments/adjustment-api.service';
import { AdjustmentFailure } from '../adjustments/adjustment.models';
import { WRITE_OFF_REASONS, WriteOffReason } from './write-off.models';
import { WriteOffStore } from './write-off.store';

const FAILURE_MESSAGES: Readonly<Record<AdjustmentFailure, MessageKey>> = {
  belowZero: 'writeOff.error.belowZero',
  conflict: 'adjustment.error.conflict',
  reason: 'writeOff.error.reason',
  notFound: 'adjustment.error.notFound',
  unknown: 'writeOff.error.unknown',
};

/**
 * إهلاك مخزون (inventory ui.md, REQ-INV-011) — **the screen that closes R9**: expired stock has
 * been visible, unsaleable and stranded in the on-hand quantity with no exit since Sprint 7.
 *
 * Two deliberate differences from the adjustment form, and only two: <b>there is no direction
 * control</b> (a write-off only removes — offering one would invent a capability nobody ruled), and
 * <b>the reason list is the write-off one</b> (DEC-INV-031). Expired batches stay selectable on
 * purpose — DEC-INV-021 keeps expired stock out of *selling*, not out of disposal.
 */
@Component({
  selector: 'app-write-off-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdjustmentApiService, WriteOffStore],
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
        <h1 class="page-title">{{ t.t('writeOff.title') }}</h1>
        <p class="page-subtitle">{{ t.t('writeOff.subtitle') }}</p>
      </header>

      <section class="form-card">
        @if (store.submit().kind === 'saved') {
          <p class="banner banner--success" role="status">
            {{ t.t('writeOff.saved') }}
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

        <vf-number-input
          [label]="t.t('writeOff.field.quantity')"
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
          <vf-button variant="primary" [disabled]="store.submit().kind === 'saving'" (pressed)="onSave()">
            {{ t.t('writeOff.action.save') }}
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
export class WriteOffPageComponent implements OnInit {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(WriteOffStore);

  protected readonly productId = signal<string | null>(null);
  protected readonly batchId = signal<string | null>(null);
  protected readonly quantity = signal<number | null>(null);
  protected readonly reason = signal<WriteOffReason | null>(null);
  protected readonly note = signal('');
  protected readonly actor = signal('');
  protected readonly submitted = signal(false);

  protected readonly productOptions = computed(() =>
    this.store.products().map((product) => ({ label: product.name, value: product.id })),
  );

  protected readonly batchOptions = computed(() =>
    this.store.batches().map((batch) => ({
      // The expiry is shown because it is usually *why* the batch is being written off — a fact
      // from the batch, never an editable field here.
      label: batch.expiryDate
        ? `${batch.batchId.split('-')[0]} · ${this.format.decimal(batch.remainingQuantity)} ${batch.stockUnitName} · ${this.format.date(batch.expiryDate)}`
        : `${batch.batchId.split('-')[0]} · ${this.format.decimal(batch.remainingQuantity)} ${batch.stockUnitName}`,
      value: batch.batchId,
    })),
  );

  protected readonly reasonOptions = computed(() =>
    WRITE_OFF_REASONS.map((reason) => ({
      label: this.t.t(`writeOff.reason.${reason}` as MessageKey),
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
    const state = this.store.submit();
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

  protected onReasonChange(value: string | null): void {
    this.reason.set((value as WriteOffReason | null) ?? null);
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
      quantity,
      reason,
      reasonNote: this.note().trim() || null,
      actorName: this.actor().trim() || null,
    });
  }
}
