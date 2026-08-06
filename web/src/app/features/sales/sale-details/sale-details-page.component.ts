import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { CommitSaleDialogComponent } from './components/commit-sale-dialog.component';
import { SaleLineItemsComponent } from './components/sale-line-items.component';
import { SaleStatusBadgeComponent } from './components/sale-status-badge.component';
import { SaleDetailsApiService } from './sale-details-api.service';
import { SaleDetailsStore } from './sale-details.store';
import { SaleLinesApiService } from './sale-lines-api.service';
import { SaleLinesStore } from './sale-lines.store';

/**
 * شاشة تفاصيل فاتورة البيع (sales ui.md, REQ-SAL-002/003) — the frozen canonical order of
 * BR-SAL-008, which is never rearranged without a UX decision: mini header (system number +
 * status badge) → invoice facts → line items → notes. The four data-view states (STD-FE-030)
 * with a distinct not-found (AC-SAL-006), RTL throughout. A literal mirror of the approved
 * purchase-details screen.
 *
 * Actions follow the status: a **draft** may add lines, remove lines and be committed (the
 * commit is disabled while it has no lines); a **committed** invoice shows none of them — it is
 * immutable and there is no path back to draft (BR-SAL-011, AC-SAL-010).
 *
 * Nothing on this screen refers to a batch (BR-SAL-013): no column, no hint, no allocation
 * detail. There is no «back to list» action because there is no sales list — DEC-SAL-005 is
 * still open and no list was invented.
 */
@Component({
  selector: 'app-sale-details-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SaleDetailsApiService, SaleDetailsStore, SaleLinesApiService, SaleLinesStore],
  imports: [
    VfButtonComponent,
    VfEmptyStateComponent,
    SaleStatusBadgeComponent,
    SaleLineItemsComponent,
    CommitSaleDialogComponent,
  ],
  template: `
    <div class="page">
      @switch (store.view().kind) {
        @case ('loading') {
          <div class="state" role="status">{{ t.t('saleDetails.loading') }}</div>
        }
        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-circle"
            [title]="t.t('saleDetails.error.title')"
            [body]="t.t('saleDetails.error.body')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
              {{ t.t('saleDetails.error.retry') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('notFound') {
          <vf-empty-state
            icon="pi-inbox"
            [title]="t.t('saleDetails.notFound.title')"
            [body]="t.t('saleDetails.notFound.body')"
          >
            <vf-button variant="secondary" icon="pi-plus" (pressed)="goToNewSale()">
              {{ t.t('saleDetails.newSale') }}
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
                <app-sale-status-badge [status]="invoice.status" />
              </div>
              <div class="header-actions">
                @if (invoice.status === 'draft') {
                  <vf-button
                    variant="primary"
                    icon="pi-check-circle"
                    [disabled]="linesStore.saving() || linesStore.lines().length === 0"
                    (pressed)="openCommit()"
                  >
                    {{ t.t('saleDetails.commit.action') }}
                  </vf-button>
                }
                <!--
                  Committed only (BR-SAL-015): a draft never consumed stock, so there is no
                  consumption trace to return along. The action does not exist for a draft rather
                  than existing and failing.
                -->
                @if (invoice.status === 'committed') {
                  <vf-button variant="secondary" icon="pi-reply" (pressed)="openReturn()">
                    {{ t.t('salesReturn.open') }}
                  </vf-button>
                }
              </div>
            </header>

            <section class="card">
              <h2 class="card-title">{{ t.t('saleDetails.section.invoice') }}</h2>
              <dl class="facts">
                <div>
                  <dt>{{ t.t('saleDetails.customer') }}</dt>
                  <!-- The customer is optional free text (DEC-SAL-002) — «—» when absent. -->
                  <dd>{{ invoice.customerName && invoice.customerName.trim() ? invoice.customerName : '—' }}</dd>
                </div>
                <div><dt>{{ t.t('saleDetails.saleDate') }}</dt><dd class="vf-num">{{ format.date(invoice.saleDate) }}</dd></div>
                <div>
                  <dt>{{ t.t('saleDetails.total') }}</dt>
                  <dd class="fact-strong vf-num">{{ format.money(invoice.total.amount, invoice.total.currency) }}</dd>
                </div>
                <div><dt>{{ t.t('saleDetails.createdAt') }}</dt><dd class="vf-num">{{ format.dateOfInstant(invoice.createdAt) }}</dd></div>
              </dl>
            </section>

            <app-sale-line-items
              [isDraft]="invoice.status === 'draft'"
              [total]="invoice.total"
              (changed)="store.retry()"
            />

            <section class="card">
              <h2 class="card-title">{{ t.t('saleDetails.section.notes') }}</h2>
              <!-- Null OR empty/whitespace always shows the standard placeholder (owner ruling 2026-07-17). -->
              <p class="notes">{{ invoice.notes && invoice.notes.trim() ? invoice.notes : t.t('saleDetails.noNotes') }}</p>
            </section>

            <app-commit-sale-dialog
              [(visible)]="commitDialogVisible"
              [saving]="linesStore.saving()"
              [rejection]="commitRejection()"
              (confirmed)="onCommitConfirm()"
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
export class SaleDetailsPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(SaleDetailsStore);
  protected readonly linesStore = inject(SaleLinesStore);
  private readonly router = inject(Router);

  /** Route parameter bound via withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly commitDialogVisible = signal(false);
  /** The classified commit refusal (ApiErrorMapper output), rendered inside the dialog (STD-UX-082). */
  protected readonly commitRejection = signal<ClassifiedFailure | null>(null);

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
        return this.t.t('saleDetails.loading');
      case 'error':
        return this.t.t('saleDetails.error.title');
      case 'notFound':
        return this.t.t('saleDetails.notFound.title');
      default:
        return view.invoice.number;
    }
  });

  protected goToNewSale(): void {
    void this.router.navigate(['/sales/new']);
  }

  protected openReturn(): void {
    const invoice = this.readyInvoice();
    if (invoice) {
      void this.router.navigate(['/sales', invoice.id, 'returns', 'new']);
    }
  }

  protected openCommit(): void {
    this.commitRejection.set(null);
    this.commitDialogVisible.set(true);
  }

  protected onCommitConfirm(): void {
    this.commitRejection.set(null);
    this.linesStore.commit((rejection) => {
      if (rejection) {
        // Nothing changed — the invoice is still a draft with all its lines (BR-SAL-012). The
        // dialog stays open and shows why; a concurrency conflict offers a retry (DEC-INV-023).
        this.commitRejection.set(rejection);
        return;
      }

      this.commitDialogVisible.set(false);
      // Re-read the header: the invoice is now Committed and immutable, so the commit and
      // line-change actions disappear (BR-SAL-011).
      this.store.retry();
    });
  }
}
