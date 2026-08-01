import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { PurchaseStatusBadgeComponent } from '../purchase-list/components/purchase-status-badge.component';
import { PurchaseLineItemsComponent } from './components/purchase-line-items.component';
import { ReceivePurchaseDialogComponent } from './components/receive-purchase-dialog.component';
import { PurchaseDetailsApiService } from './purchase-details-api.service';
import { PurchaseDetailsStore } from './purchase-details.store';
import { PurchaseLinesApiService } from './purchase-lines-api.service';
import { PurchaseLinesStore } from './purchase-lines.store';
import { ReceivePurchaseInvoicePayload } from './purchase-lines.models';

/**
 * تفاصيل فاتورة الشراء (purchasing ui.md, REQ-PUR-002): a read-only page showing
 * the complete invoice header — the four data-view states (STD-FE-030) including a
 * distinct not-found, RTL throughout. Mirrors the Catalog product-details screen;
 * no editing, receiving, or inventory (scope lock, Slice 2).
 */
@Component({
  selector: 'app-purchase-details-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchaseDetailsApiService, PurchaseDetailsStore, PurchaseLinesApiService, PurchaseLinesStore],
  imports: [
    VfButtonComponent,
    VfEmptyStateComponent,
    PurchaseStatusBadgeComponent,
    PurchaseLineItemsComponent,
    ReceivePurchaseDialogComponent,
  ],
  template: `
    <div class="page">
      @switch (store.view().kind) {
        @case ('loading') {
          <div class="state" role="status">{{ t.t('purchaseDetails.loading') }}</div>
        }
        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-circle"
            [title]="t.t('purchaseDetails.error.title')"
            [body]="t.t('purchaseDetails.error.body')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
              {{ t.t('purchaseDetails.error.retry') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('notFound') {
          <vf-empty-state
            icon="pi-inbox"
            [title]="t.t('purchaseDetails.notFound.title')"
            [body]="t.t('purchaseDetails.notFound.body')"
          >
            <vf-button variant="secondary" icon="pi-arrow-right" (pressed)="goToList()">
              {{ t.t('purchaseDetails.back') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('ready') {
          @if (readyInvoice(); as invoice) {
            <header class="header">
              <div class="titles">
                <h1 class="number vf-num">{{ invoice.number }}</h1>
              </div>
              <div class="badges">
                <app-purchase-status-badge [status]="invoice.status" />
              </div>
              <div class="header-actions">
                @if (invoice.status === 'draft') {
                  <vf-button variant="primary" icon="pi-check-circle" [disabled]="linesStore.saving()" (pressed)="openReceive()">
                    {{ t.t('purchaseDetails.receive.action') }}
                  </vf-button>
                }
                <!--
                  Received only (BR-PUR-015): a draft never put stock in and a cancelled invoice
                  never will, so the action does not exist for them rather than existing and failing.
                -->
                @if (invoice.status === 'received') {
                  <vf-button variant="secondary" icon="pi-reply" (pressed)="openReturn()">
                    {{ t.t('purchaseReturn.open') }}
                  </vf-button>
                }
                <vf-button variant="quiet" icon="pi-arrow-right" (pressed)="goToList()">
                  {{ t.t('purchaseDetails.back') }}
                </vf-button>
              </div>
            </header>

            <section class="card">
              <h2 class="card-title">{{ t.t('purchaseDetails.section.invoice') }}</h2>
              <dl class="facts">
                <div><dt>{{ t.t('purchaseDetails.supplier') }}</dt><dd>{{ invoice.supplierName }}</dd></div>
                <div>
                  <dt>{{ t.t('purchaseDetails.reference') }}</dt>
                  <dd><bdi>{{ invoice.supplierInvoiceReference ?? '—' }}</bdi></dd>
                </div>
                <div><dt>{{ t.t('purchaseDetails.invoiceDate') }}</dt><dd class="vf-num">{{ format.date(invoice.invoiceDate) }}</dd></div>
                <div>
                  <dt>{{ t.t('purchaseDetails.total') }}</dt>
                  <dd class="fact-strong vf-num">{{ format.money(invoice.total.amount, invoice.total.currency) }}</dd>
                </div>
                <div><dt>{{ t.t('purchaseDetails.createdAt') }}</dt><dd class="vf-num">{{ format.date(invoice.createdAt.slice(0, 10)) }}</dd></div>
              </dl>
            </section>

            <app-purchase-line-items
              [isDraft]="invoice.status === 'draft'"
              [total]="invoice.total"
              (changed)="store.retry()"
            />

            <section class="card">
              <h2 class="card-title">{{ t.t('purchaseDetails.section.notes') }}</h2>
              <!-- Null OR empty/whitespace always shows the standard placeholder (owner ruling 2026-07-17). -->
              <p class="notes">{{ invoice.notes && invoice.notes.trim() ? invoice.notes : t.t('purchaseDetails.noNotes') }}</p>
            </section>

            <app-receive-purchase-dialog
              [(visible)]="receiveDialogVisible"
              [lines]="linesStore.lines()"
              [saving]="linesStore.saving()"
              [serverFailure]="receiveServerFailure()"
              (confirmed)="onReceiveConfirm($event)"
            />
          }
        }
      }

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
      max-inline-size: var(--vf-content-max-width);
      inline-size: 100%;
      margin-inline: auto;
      padding: var(--vf-space-5) var(--vf-space-6);
    }

    .state {
      padding: var(--vf-space-7);
      text-align: center;
      color: var(--vf-text-secondary);
    }

    .header {
      display: flex;
      align-items: flex-start;
      gap: var(--vf-space-3);
      flex-wrap: wrap;
    }

    .titles {
      flex: 1;
      min-inline-size: 12rem;
    }

    .number {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .badges {
      display: flex;
      gap: var(--vf-space-2);
      align-items: center;
      flex-wrap: wrap;
    }

    .header-actions {
      display: flex;
      gap: var(--vf-space-2);
      align-items: center;
    }

    .card {
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      padding: var(--vf-space-4) var(--vf-space-5);
    }

    .card-title {
      margin: 0 0 var(--vf-space-3);
      font-size: var(--vf-text-section-title);
      font-weight: 600;
    }

    .facts {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      gap: var(--vf-space-3);
      margin: 0;
    }

    .facts dt {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      margin-block-end: var(--vf-space-1);
    }

    .facts dd {
      margin: 0;
      color: var(--vf-text);
    }

    .fact-strong {
      font-weight: 600;
    }

    .notes {
      margin: 0;
      color: var(--vf-text-secondary);
      white-space: pre-wrap;
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }
    }
  `,
})
export class PurchaseDetailsPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(PurchaseDetailsStore);
  protected readonly linesStore = inject(PurchaseLinesStore);
  private readonly router = inject(Router);

  /** Route parameter bound via withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly receiveDialogVisible = signal(false);
  /** The classified receive failure, rendered inside the dialog (STD-UX-082/037). */
  protected readonly receiveServerFailure = signal<ClassifiedFailure | null>(null);

  constructor() {
    effect(() => {
      const id = this.id();
      this.store.setId(id);
      this.linesStore.setId(id);
    });
  }

  protected readyInvoice() {
    const view = this.store.view();
    return view.kind === 'ready' ? view.invoice : null;
  }

  // Screen-level load outcomes for the polite live region (STD-UX-092):
  // loading, error, not-found, and the loaded invoice by number.
  protected readonly announcement = computed(() => {
    const view = this.store.view();
    switch (view.kind) {
      case 'loading':
        return this.t.t('purchaseDetails.loading');
      case 'error':
        return this.t.t('purchaseDetails.error.title');
      case 'notFound':
        return this.t.t('purchaseDetails.notFound.title');
      default:
        return view.invoice.number;
    }
  });

  protected goToList(): void {
    void this.router.navigate(['/purchases']);
  }

  /** Open the new-purchase-return screen for this invoice (REQ-PUR-006, purchasing/ui.md). */
  protected openReturn(): void {
    void this.router.navigate(['/purchases', this.id(), 'returns', 'new']);
  }

  protected openReceive(): void {
    this.receiveServerFailure.set(null);
    this.receiveDialogVisible.set(true);
  }

  protected onReceiveConfirm(payload: ReceivePurchaseInvoicePayload): void {
    this.receiveServerFailure.set(null);
    this.linesStore.receive(payload, (failure) => {
      if (failure) {
        // Classified by the shared mapper (STD-UX-123, STD-UX-037): the ruled
        // per-code wording, never one generic sentence for an irreversible
        // stock operation.
        this.receiveServerFailure.set(failure);
      } else {
        this.receiveDialogVisible.set(false);
        // Re-read the header: the invoice is now Received and immutable, so the receive
        // and line-change actions disappear (BR-PUR-011).
        this.store.retry();
      }
    });
  }
}
