import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';
import { RuleMessageOverrides } from '../../../core/validation/validation-messages';
import { vfValidators } from '../../../core/validation/validators';
import { VfBannerComponent } from '../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfFormFieldComponent } from '../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { VfSelectComponent } from '../../../shared/ui-kit/select/vf-select.component';
import { AdjustmentApiService } from './adjustment-api.service';
import { AdjustmentStore } from './adjustment.store';
import { ADJUSTMENT_REASONS, AdjustmentDirection, AdjustmentReason } from './adjustment.models';

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
 *
 * Phase 1 proof-of-concept of the validation foundation
 * (validation-and-guidance.md): a typed reactive form (STD-UX-126) whose
 * fields render through `vf-form-field` with the ruled per-field wordings as
 * overrides, submit guidance with first-invalid focus (STD-UX-122), the
 * shared ApiErrorMapper in the store (STD-UX-123), a focusable rejection
 * banner (STD-UX-071) that clears on any relevant edit (STD-UX-035), and a
 * retry-labelled primary action on a concurrency conflict (STD-UX-033).
 * Adoption Epic additions: the shared `vf-banner` (STD-UX-121) and surfaced
 * picker-load failures with retry (STD-UX-041).
 */
@Component({
  selector: 'app-adjustment-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdjustmentApiService, AdjustmentStore],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfFormFieldComponent,
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

      <form class="form-card" [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="save()">
        @if (submitState().kind === 'saved') {
          <vf-banner tone="success">
            {{ t.t('adjustment.saved') }}
            <a class="history-link" routerLink="/inventory/history">{{ t.t('adjustment.saved.link') }}</a>
          </vf-banner>
        }

        @if (failureMessage(); as message) {
          <vf-banner tone="error" #failureBanner>{{ message }}</vf-banner>
        }

        <vf-form-field
          [label]="t.t('adjustment.field.product')"
          [required]="true"
          [messages]="productMessages"
        >
          <vf-select
            [formControl]="form.controls.productId"
            [filterable]="true"
            [placeholder]="t.t('adjustment.field.productPlaceholder')"
            [optionList]="productOptions()"
          />
        </vf-form-field>
        @if (store.productsError()) {
          <vf-banner tone="error">
            {{ t.t('pickers.productsError') }}
            <button type="button" class="retry-link" (click)="store.loadProducts()">
              {{ t.t('errors.retry') }}
            </button>
          </vf-banner>
        }

        <vf-form-field [label]="t.t('adjustment.field.batch')" [required]="true" [messages]="batchMessages">
          <vf-select
            [formControl]="form.controls.batchId"
            [placeholder]="batchPlaceholder()"
            [optionList]="batchOptions()"
          />
        </vf-form-field>
        @if (store.batchesError()) {
          <vf-banner tone="error">
            {{ t.t('pickers.batchesError') }}
            <button type="button" class="retry-link" (click)="store.loadBatches(form.controls.productId.value)">
              {{ t.t('errors.retry') }}
            </button>
          </vf-banner>
        }

        <vf-form-field [label]="t.t('adjustment.field.direction')" [required]="true">
          <vf-select [formControl]="form.controls.direction" [clearable]="false" [optionList]="directionOptions()" />
        </vf-form-field>

        <vf-form-field
          [label]="t.t('adjustment.field.quantity')"
          [required]="true"
          [messages]="quantityMessages"
        >
          <vf-number-input [formControl]="form.controls.quantity" />
        </vf-form-field>

        <vf-form-field [label]="t.t('adjustment.field.reason')" [required]="true" [messages]="reasonMessages">
          <vf-select
            [formControl]="form.controls.reason"
            [placeholder]="t.t('adjustment.field.reasonPlaceholder')"
            [optionList]="reasonOptions()"
          />
        </vf-form-field>

        <vf-form-field [label]="t.t('adjustment.field.note')">
          <vf-textarea [formControl]="form.controls.note" [rows]="3" />
        </vf-form-field>

        <vf-form-field [label]="t.t('adjustment.field.actor')" [hint]="t.t('adjustment.field.actorHint')">
          <vf-text-input
            [formControl]="form.controls.actor"
            [placeholder]="t.t('adjustment.field.actorPlaceholder')"
          />
        </vf-form-field>

        <div class="actions">
          <vf-button variant="primary" type="submit" [disabled]="submitState().kind === 'saving'">
            {{ saveLabel() }}
          </vf-button>
        </div>
      </form>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
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

    .page-subtitle {
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

    .history-link {
      color: inherit;
      font-weight: 600;
    }

    .retry-link {
      border: none;
      background: none;
      padding: 0;
      color: inherit;
      font: inherit;
      font-weight: 600;
      text-decoration: underline;
      cursor: pointer;
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
  private readonly focus = inject(ValidationFocusService);

  // Typed reactive form (STD-FE-016, STD-UX-126). The validators mirror the
  // documented rules only (STD-UX-021): required per REQ-INV-010's mandatory
  // inputs, positive per BR-INV-064 (the sign belongs to the server); note
  // and actor are deliberately unvalidated (DEC-INV-030).
  protected readonly form = new FormGroup({
    productId: new FormControl<string | null>(null, vfValidators.required),
    batchId: new FormControl<string | null>(null, vfValidators.required),
    direction: new FormControl<AdjustmentDirection>('increase', { nonNullable: true }),
    quantity: new FormControl<number | null>(null, [vfValidators.required, vfValidators.positive]),
    reason: new FormControl<AdjustmentReason | null>(null, vfValidators.required),
    note: new FormControl('', { nonNullable: true }),
    actor: new FormControl('', { nonNullable: true }),
  });

  // The ruled per-field wordings (inventory ui.md) as overrides (STD-UX-111).
  protected readonly productMessages: RuleMessageOverrides = { required: 'adjustment.error.productRequired' };
  protected readonly batchMessages: RuleMessageOverrides = { required: 'adjustment.error.batchRequired' };
  protected readonly quantityMessages: RuleMessageOverrides = {
    required: 'adjustment.error.quantityPositive',
    positive: 'adjustment.error.quantityPositive',
  };
  protected readonly reasonMessages: RuleMessageOverrides = { required: 'adjustment.error.reasonRequired' };

  protected readonly submitState = this.store.submit;

  private readonly failureBanner = viewChild('failureBanner', { read: ElementRef });

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
    { label: this.t.t('adjustment.direction.increase'), value: 'increase' as AdjustmentDirection },
    { label: this.t.t('adjustment.direction.decrease'), value: 'decrease' as AdjustmentDirection },
  ]);

  protected readonly reasonOptions = computed(() =>
    ADJUSTMENT_REASONS.map((reason) => ({
      label: this.t.t(`adjustment.reason.${reason}`),
      value: reason,
    })),
  );

  protected readonly batchPlaceholder = computed(() =>
    this.store.batchesLoading()
      ? this.t.t('adjustment.field.batchLoading')
      : this.form.controls.productId.value
        ? this.t.t('adjustment.field.batchPlaceholder')
        : this.t.t('adjustment.field.batchPickProduct'),
  );

  protected readonly failureMessage = computed(() => {
    const state = this.submitState();
    return state.kind === 'failed' ? this.t.t(state.failure.messageKey, state.failure.params) : null;
  });

  // Screen-level outcomes for the polite live region (STD-UX-092): the saved
  // fact (its banner is a status insertion, which alone is not reliably
  // announced) and the dependent-batches load. Rejections announce through
  // their `role="alert"` surfaces.
  protected readonly announcement = computed(() => {
    if (this.submitState().kind === 'saved') {
      return this.t.t('adjustment.saved');
    }

    return this.store.batchesLoading() ? this.t.t('adjustment.field.batchLoading') : '';
  });

  // A concurrency conflict re-offers the same action as a retry (STD-UX-033, BR-INV-068).
  protected readonly saveLabel = computed(() => {
    const state = this.submitState();
    return state.kind === 'failed' && state.failure.retryable
      ? this.t.t('errors.retry')
      : this.t.t('adjustment.action.save');
  });

  constructor() {
    // The picker of batches follows the chosen product; changing it always
    // clears the dependent choice.
    this.form.controls.productId.valueChanges.pipe(takeUntilDestroyed()).subscribe((productId) => {
      this.form.controls.batchId.setValue(null);
      this.store.loadBatches(productId);
    });

    // A rejection banner never survives the edit that addresses it
    // (STD-UX-035); an edit after success starts a fresh operation.
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      const kind = this.store.submit().kind;
      if (kind === 'failed' || kind === 'saved') {
        this.store.reset();
      }
    });

    // An operation-level rejection has no field target: the banner itself
    // receives focus and is announced (STD-UX-071).
    effect(() => {
      if (this.submitState().kind !== 'failed') {
        return;
      }

      const banner = this.failureBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  ngOnInit(): void {
    this.store.loadProducts();
  }

  protected save(): void {
    const { productId, batchId, direction, quantity, reason, note, actor } = this.form.getRawValue();
    // The guidance emits validSubmit only on a valid form; the guard keeps the
    // types honest without a non-null assertion (STD-FE-022).
    if (!productId || !batchId || quantity === null || !reason) {
      return;
    }

    this.store.save({
      batchId,
      direction,
      quantity,
      reason,
      reasonNote: note.trim() || null,
      actorName: actor.trim() || null,
    });
  }
}
