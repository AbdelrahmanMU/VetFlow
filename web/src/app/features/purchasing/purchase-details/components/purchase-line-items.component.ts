import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { ValidationFocusService } from '../../../../core/validation/validation-focus.service';
import { VfBannerComponent } from '../../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { Money } from '../purchase-details.models';
import { PurchaseLinesApiService } from '../purchase-lines-api.service';
import { AddPurchaseLinePayload, ProductPickerOption } from '../purchase-lines.models';
import { PurchaseLinesStore } from '../purchase-lines.store';
import { AddPurchaseLineDialogComponent } from './add-purchase-line-dialog.component';

/**
 * قسم بنود الفاتورة داخل شاشة التفاصيل (purchasing ui.md, REQ-PUR-004): lists the
 * invoice's line items (table on desktop, stacked cards on mobile — RTL, zero
 * overflow), shows the server-derived invoice total (never computed here —
 * BR-PUR-006/DEC-PUR-003), and — for a draft only (AC-PUR-012) — offers add (dialog)
 * and remove. A change emits {@link changed} so the page re-reads the header total.
 */
@Component({
  selector: 'app-purchase-line-items',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBannerComponent, VfButtonComponent, VfEmptyStateComponent, AddPurchaseLineDialogComponent],
  template: `
    <section class="card">
      <div class="card-head">
        <h2 class="card-title">{{ t.t('purchaseDetails.lines.title') }}</h2>
        @if (isDraft()) {
          <vf-button variant="secondary" icon="pi-plus" [disabled]="store.saving()" (pressed)="openDialog()">
            {{ t.t('purchaseDetails.lines.add') }}
          </vf-button>
        }
      </div>

      @if (removeFailureMessage(); as message) {
        <vf-banner tone="error" class="remove-banner" #removeBanner>{{ message }}</vf-banner>
      }

      @switch (store.view().kind) {
        @case ('loading') {
          <div class="state" role="status">{{ t.t('purchaseDetails.lines.loading') }}</div>
        }
        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-circle"
            [title]="t.t('purchaseDetails.lines.error.title')"
            [body]="t.t('purchaseDetails.lines.error.body')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="store.refresh()">
              {{ t.t('purchaseDetails.lines.error.retry') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('ready') {
          @if (lines(); as rows) {
            @if (rows.length === 0) {
              <vf-empty-state
                icon="pi-inbox"
                [title]="t.t('purchaseDetails.lines.empty.title')"
                [body]="isDraft() ? t.t('purchaseDetails.lines.empty.draftBody') : t.t('purchaseDetails.lines.empty.body')"
              />
            } @else {
              <div class="lines-scroll">
                <table class="lines">
                  <thead>
                    <tr>
                      <th>{{ t.t('purchaseDetails.lines.col.product') }}</th>
                      <th>{{ t.t('purchaseDetails.lines.col.unit') }}</th>
                      <th>{{ t.t('purchaseDetails.lines.col.quantity') }}</th>
                      <th>{{ t.t('purchaseDetails.lines.col.unitPrice') }}</th>
                      <th>{{ t.t('purchaseDetails.lines.col.lineTotal') }}</th>
                      <th class="action-col"><span class="sr-only">{{ t.t('purchaseDetails.lines.col.actions') }}</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (row of rows; track row.id) {
                      <tr>
                        <td [attr.data-label]="t.t('purchaseDetails.lines.col.product')">{{ row.productName }}</td>
                        <td [attr.data-label]="t.t('purchaseDetails.lines.col.unit')">{{ row.purchaseUnitName }}</td>
                        <td [attr.data-label]="t.t('purchaseDetails.lines.col.quantity')" class="vf-num">
                          {{ format.decimal(row.quantity) }}
                        </td>
                        <td [attr.data-label]="t.t('purchaseDetails.lines.col.unitPrice')" class="vf-num">
                          {{ format.money(row.unitPrice.amount, row.unitPrice.currency) }}
                        </td>
                        <td [attr.data-label]="t.t('purchaseDetails.lines.col.lineTotal')" class="vf-num fact-strong">
                          {{ format.money(row.lineTotal.amount, row.lineTotal.currency) }}
                        </td>
                        <td class="action-col">
                          @if (isDraft()) {
                            <vf-button
                              variant="quiet"
                              icon="pi-trash"
                              [disabled]="store.saving()"
                              (pressed)="remove(row.id)"
                            >
                              {{ t.t('purchaseDetails.lines.remove') }}
                            </vf-button>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }

            <div class="total-row">
              <span>{{ t.t('purchaseDetails.lines.total') }}</span>
              <span class="total-value vf-num">{{ format.money(total().amount, total().currency) }}</span>
            </div>
          }
        }
      }

      <app-add-purchase-line-dialog
        [(visible)]="dialogVisible"
        [products]="products()"
        [saving]="store.saving()"
        [serverFailure]="serverFailure()"
        [productsLoadFailed]="productsError()"
        (retryProducts)="loadProducts()"
        (save)="onSave($event)"
      />
    </section>
  `,
  styles: `
    @use '../../../../shared/styles/numeric' as numeric;

    .card {
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-4) var(--vf-space-5);
    }

    .card-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
      margin-block-end: var(--vf-space-3);
      flex-wrap: wrap;
    }

    /* Instance spacing only — the banner's own chrome is the shared component's (STD-UX-121). */
    .remove-banner {
      margin-block-end: var(--vf-space-3);
    }

    .card-title {
      margin: 0;
      font-size: var(--vf-text-section-title);
      font-weight: 600;
    }

    .state {
      padding: var(--vf-space-4);
      text-align: center;
      color: var(--vf-text-secondary);
    }

    .lines-scroll {
      overflow-x: auto;
    }

    .lines {
      inline-size: 100%;
      border-collapse: collapse;
    }

    .lines th,
    .lines td {
      padding: var(--vf-space-2) var(--vf-space-3);
      text-align: start;
      border-block-end: 1px solid var(--vf-border);
    }

    .lines th {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      font-weight: 600;
    }

    /* Quantity, unit price and line total follow the one numeric standard (§6). */
    .lines {
      @include numeric.cells;
    }

    .fact-strong {
      font-weight: 600;
    }

    .action-col {
      text-align: end;
    }

    .sr-only {
      position: absolute;
      inline-size: 1px;
      block-size: 1px;
      overflow: hidden;
      clip: rect(0 0 0 0);
      white-space: nowrap;
    }

    .total-row {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: var(--vf-space-3);
      margin-block-start: var(--vf-space-3);
      padding-block-start: var(--vf-space-3);
      border-block-start: 1px solid var(--vf-border);
    }

    .total-value {
      font-size: var(--vf-text-section-title);
      font-weight: 700;
    }

    @media (max-width: 768px) {
      .lines thead {
        position: absolute;
        inline-size: 1px;
        block-size: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
      }

      .lines,
      .lines tbody,
      .lines tr,
      .lines td {
        display: block;
        inline-size: 100%;
      }

      .lines tr {
        border: 1px solid var(--vf-border);
        border-radius: var(--vf-radius-small);
        margin-block-end: var(--vf-space-2);
        padding: var(--vf-space-2) var(--vf-space-3);
      }

      .lines td {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: var(--vf-space-3);
        border: 0;
        padding-inline: 0;
        padding-block: var(--vf-space-1);
      }

      .lines td[data-label]::before {
        content: attr(data-label);
        color: var(--vf-text-faint);
        font-size: var(--vf-text-caption);
      }

      .lines td.action-col {
        justify-content: flex-end;
      }
    }
  `,
})
export class PurchaseLineItemsComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(PurchaseLinesStore);
  private readonly api = inject(PurchaseLinesApiService);
  private readonly focus = inject(ValidationFocusService);

  readonly isDraft = input.required<boolean>();
  readonly total = input.required<Money>();
  readonly changed = output<void>();

  protected readonly dialogVisible = signal(false);
  protected readonly products = signal<readonly ProductPickerOption[]>([]);
  /** The classified add failure, rendered inside the dialog (STD-UX-082). */
  protected readonly serverFailure = signal<ClassifiedFailure | null>(null);
  /** A failed removal is surfaced in the card, never silent (STD-UX-004). */
  protected readonly removeFailure = signal<ClassifiedFailure | null>(null);
  protected readonly productsError = signal(false);
  private readonly removeBanner = viewChild('removeBanner', { read: ElementRef });
  private productsLoaded = false;

  protected readonly lines = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view.lines : null;
  });

  protected readonly removeFailureMessage = computed(() => {
    const failure = this.removeFailure();
    return failure ? this.t.t(failure.messageKey, failure.params) : null;
  });

  constructor() {
    // The removal banner receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.removeFailure()) {
        return;
      }

      const banner = this.removeBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  protected loadProducts(): void {
    this.productsError.set(false);
    this.api.getActiveProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.productsLoaded = true;
      },
      // A failed load is surfaced in the dialog with a retry (STD-UX-041).
      error: () => this.productsError.set(true),
    });
  }

  protected openDialog(): void {
    this.serverFailure.set(null);
    if (!this.productsLoaded) {
      this.loadProducts();
    }

    this.dialogVisible.set(true);
  }

  protected onSave(payload: AddPurchaseLinePayload): void {
    this.serverFailure.set(null);
    this.store.add(payload, (failure) => {
      if (failure) {
        this.serverFailure.set(failure);
      } else {
        this.dialogVisible.set(false);
        this.changed.emit();
      }
    });
  }

  protected remove(lineId: string): void {
    this.removeFailure.set(null);
    this.store.remove(lineId, (failure) => {
      if (failure) {
        this.removeFailure.set(failure);
      } else {
        this.changed.emit();
      }
    });
  }
}
