import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MessageKey } from '../../../core/i18n/ar';
import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { PurchaseReturnApiService } from './purchase-return-api.service';
import { PurchaseReturnStore } from './purchase-return.store';
import { PurchaseReturnFailure } from './purchase-return.models';

const FAILURE_MESSAGES: Readonly<Record<PurchaseReturnFailure, MessageKey>> = {
  invoiceNotReceived: 'purchaseReturn.error.invoiceNotReceived',
  exceedsReturnable: 'purchaseReturn.error.exceedsReturnable',
  notDraft: 'purchaseReturn.error.notDraft',
  noLines: 'purchaseReturn.error.noLines',
  belowZero: 'purchaseReturn.error.belowZero',
  conflict: 'purchaseReturn.error.conflict',
  notFound: 'purchaseReturn.error.notFound',
  unknown: 'purchaseReturn.error.unknown',
};

/**
 * مرتجع مشتريات جديد (purchasing ui.md, REQ-PUR-006, DEC-PUR-010).
 *
 * <b>Three controls the rules say must not exist here, and do not:</b> there is <b>no reason
 * field</b> (BR-INV-067 — «مستندها هو سياقها»), <b>no batch picker</b> (the destination is derived
 * from the original line — BR-PUR-017 — so offering a choice would imply a capability nobody
 * ruled), and <b>no amount or total</b> (a return is a stock movement only — DEC-INV-035 — and a
 * number with a currency beside it would suggest a credit that does not exist).
 *
 * The screen is reachable only from a <b>received</b> invoice (BR-PUR-015); the read itself 404s
 * otherwise, and that is rendered as a plain "nothing to return here" rather than an error.
 */
@Component({
  selector: 'app-purchase-return-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchaseReturnApiService, PurchaseReturnStore],
  imports: [FormsModule, RouterLink, VfNumberInputComponent, VfTextareaComponent, VfButtonComponent],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('purchaseReturn.title') }}</h1>
        <p class="page-subtitle">{{ t.t('purchaseReturn.subtitle') }}</p>
      </header>

      @if (store.submit().kind === 'saved') {
        <div class="banner banner--success" role="status">
          <span>{{ t.t('purchaseReturn.saved') }}</span>
          <strong>{{ t.t('purchaseReturn.savedNumber') }}: {{ savedNumber() }}</strong>
          <a [routerLink]="['/purchases', invoiceId()]">{{ t.t('purchaseReturn.backToInvoice') }}</a>
        </div>
      } @else if (store.unavailable()) {
        <p class="empty-state">{{ t.t('purchaseReturn.unavailable') }}</p>
      } @else if (store.linesLoading()) {
        <p class="empty-state">{{ t.t('purchaseReturn.loading') }}</p>
      } @else if (store.lines().length === 0) {
        <p class="empty-state">{{ t.t('purchaseReturn.empty') }}</p>
      } @else {
        @if (errorMessage(); as message) {
          <div class="banner banner--error" role="alert">{{ message }}</div>
        }

        <div class="field">
          <label for="returnDate">{{ t.t('purchaseReturn.field.returnDate') }}</label>
          <input id="returnDate" type="date" [(ngModel)]="returnDate" />
        </div>

        <table class="vf-table">
          <thead>
            <tr>
              <th scope="col">{{ t.t('purchaseReturn.column.product') }}</th>
              <th scope="col">{{ t.t('purchaseReturn.column.originalQuantity') }}</th>
              <th scope="col">{{ t.t('purchaseReturn.column.returnable') }}</th>
              <th scope="col">{{ t.t('purchaseReturn.column.returnQuantity') }}</th>
            </tr>
          </thead>
          <tbody>
            @for (line of store.lines(); track line.purchaseLineItemId) {
              <tr>
                <td>{{ line.productName }}</td>
                <td>{{ format.decimal(line.quantity) }} {{ line.purchaseUnitName }}</td>
                <td>{{ format.decimal(line.returnableQuantity) }} {{ line.purchaseUnitName }}</td>
                <td>
                  <!--
                    The returnable remainder is enforced by the server (BR-PUR-016), the only place
                    that can see the committed returns. The min bound spares an obvious round trip;
                    there is deliberately no client-side ceiling pretending to be the rule.
                  -->
                  <vf-number-input
                    [ngModel]="quantityOf(line.purchaseLineItemId)"
                    (ngModelChange)="setQuantity(line.purchaseLineItemId, $event)"
                    [min]="0"
                  />
                </td>
              </tr>
            }
          </tbody>
        </table>

        <vf-textarea [label]="t.t('purchaseReturn.field.notes')" [(ngModel)]="notes" name="notes" />

        <div class="form-actions">
          <vf-button
            variant="primary"
            [disabled]="saving() || !hasQuantity()"
            (pressed)="save()"
          >
            {{ saving() ? t.t('purchaseReturn.saving') : t.t('purchaseReturn.save') }}
          </vf-button>
          <a class="vf-link" [routerLink]="['/purchases', invoiceId()]">{{ t.t('purchaseReturn.cancel') }}</a>
        </div>
      }
    </div>
  `,
  // Added when C6's live-browser pass covered this screen too — C5 shipped without one. Every class
  // this template uses was defined nowhere, so the banners and page chrome rendered as plain
  // unstyled text: `.banner`/`.banner-success` exist in no stylesheet, and the UI Kit's `.vf-table`
  // rules are ::ng-deep-scoped inside <vf-table> and never reach a raw table. Tokens are the real
  // ones (--vf-success-soft / --vf-success), the C4 lesson: a token that does not exist fails
  // silently through the CSS fallback while Stylelint passes either way.
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
    .empty-state {
      margin: 0;
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-caption);
    }

    .banner {
      margin: 0;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius);
      font-size: var(--vf-text-caption);
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .banner--error {
      background: var(--vf-danger-soft);
      color: var(--vf-danger);
    }

    .banner--success {
      background: var(--vf-success-soft);
      color: var(--vf-success);
    }

    .banner a {
      color: inherit;
      font-weight: 600;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      max-inline-size: 20rem;
    }

    .field label {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
    }

    .field input {
      padding: var(--vf-space-2) var(--vf-space-3);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      background: var(--vf-surface);
      color: var(--vf-text);
      font-family: inherit;
      font-size: inherit;
    }

    /*
      A short editable form table, not a data list: the UI Kit's <vf-table> is a scrollable PrimeNG
      datatable with state storage, built for lists rather than for a handful of rows each carrying
      an input. Styled here to the same table language rather than restructured — the deviation is
      reported, not hidden.
    */
    .vf-table {
      inline-size: 100%;
      border-collapse: collapse;
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      overflow: hidden;
    }

    .vf-table th,
    .vf-table td {
      padding: var(--vf-space-3);
      text-align: start;
      border-block-end: 1px solid var(--vf-border);
    }

    .vf-table th {
      background: var(--vf-bg);
      font-size: var(--vf-text-caption);
      color: var(--vf-text-secondary);
      font-weight: 600;
    }

    .vf-table tbody tr:last-child td {
      border-block-end: none;
    }

    .form-actions {
      display: flex;
      align-items: center;
      gap: var(--vf-space-4);
    }

    .vf-link {
      color: var(--vf-primary);
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }

      /* The four columns stay readable on a phone instead of forcing the page to scroll. */
      .vf-table {
        display: block;
        overflow-x: auto;
      }
    }
  `,
})
export class PurchaseReturnPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);

  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(PurchaseReturnStore);

  protected readonly invoiceId = signal('');
  protected returnDate = '';
  protected notes = '';

  private readonly quantities = signal<ReadonlyMap<string, number>>(new Map());

  protected readonly saving = computed(() => this.store.submit().kind === 'saving');

  protected readonly savedNumber = computed(() => {
    const state = this.store.submit();
    return state.kind === 'saved' ? state.number : '';
  });

  protected readonly hasQuantity = computed(() =>
    [...this.quantities().values()].some((quantity) => quantity > 0),
  );

  protected readonly errorMessage = computed(() => {
    const state = this.store.submit();
    return state.kind === 'failed' ? this.t.t(FAILURE_MESSAGES[state.failure]) : null;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.invoiceId.set(id);
    // The return date defaults to the clinic's today as the browser sees it; the business date
    // basis itself is the server's (BR-INV-059/060), which is why nothing is derived from it here.
    this.returnDate = new Date().toISOString().slice(0, 10);
    if (id) {
      this.store.loadLines(id);
    }
  }

  protected quantityOf(lineId: string): number {
    return this.quantities().get(lineId) ?? 0;
  }

  protected setQuantity(lineId: string, quantity: number | null): void {
    const next = new Map(this.quantities());
    next.set(lineId, quantity ?? 0);
    this.quantities.set(next);
  }

  protected save(): void {
    this.store.save(
      this.invoiceId(),
      this.returnDate,
      this.notes.trim() === '' ? null : this.notes.trim(),
      this.quantities(),
    );
  }
}
