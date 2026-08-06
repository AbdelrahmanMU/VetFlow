import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  FormRecord,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { SubmitGuidanceDirective } from '../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';
import { RuleMessageOverrides } from '../../../core/validation/validation-messages';
import { vfValidators } from '../../../core/validation/validators';
import { VfBannerComponent } from '../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfFormFieldComponent } from '../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfDateInputComponent } from '../../../shared/ui-kit/input/vf-date-input.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { PurchaseReturnApiService } from './purchase-return-api.service';
import { PurchaseReturnStore } from './purchase-return.store';

/**
 * A return-quantity cell accepts empty or ≥ 0 — zero simply skips the line;
 * a chosen line must be > 0 (VTF-PUR-017, enforced server-side per line).
 * Screen-local because the frozen shared library (Foundation v1) has no
 * non-negative shape; recorded as debt for the next Foundation window.
 */
function nonNegative(control: AbstractControl): ValidationErrors | null {
  const value: unknown = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }

  return typeof value === 'number' && value >= 0 ? null : { nonNegative: true };
}

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
 *
 * Validation-foundation adoption (validation-and-guidance.md, Adoption Epic):
 * a typed reactive form (STD-UX-126) — the date and each line's quantity run
 * the three moments through `vf-form-field` (STD-UX-120); submit stays
 * enabled and runs the shared guidance (STD-UX-016/122); the classified
 * failure renders in the shared banner, focused on appearance (STD-UX-071),
 * clearing on the next relevant edit (STD-UX-035), and — per STD-UX-042 —
 * states the partial document state when the multi-step save failed after
 * the draft was created.
 */
@Component({
  selector: 'app-purchase-return-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PurchaseReturnApiService, PurchaseReturnStore],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfDialogComponent,
    VfFormFieldComponent,
    VfDateInputComponent,
    VfNumberInputComponent,
    VfTextareaComponent,
    VfButtonComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('purchaseReturn.title') }}</h1>
        <p class="page-subtitle">{{ t.t('purchaseReturn.subtitle') }}</p>
      </header>

      @if (store.submit().kind === 'saved') {
        <vf-banner tone="success" class="saved-banner">
          <span>{{ t.t('purchaseReturn.saved') }}</span>
          <strong>{{ t.t('purchaseReturn.savedNumber') }}: {{ savedNumber() }}</strong>
          <a [routerLink]="['/purchases', invoiceId()]">{{ t.t('purchaseReturn.backToInvoice') }}</a>
        </vf-banner>
      } @else if (store.unavailable()) {
        <p class="empty-state">{{ t.t('purchaseReturn.unavailable') }}</p>
      } @else if (store.linesLoading()) {
        <p class="empty-state">{{ t.t('purchaseReturn.loading') }}</p>
      } @else if (store.lines().length === 0) {
        <p class="empty-state">{{ t.t('purchaseReturn.empty') }}</p>
      } @else {
        <!--
          Committing is irreversible and moves stock, so it is confirmed explicitly
          (purchasing ui.md §«مرتجع مشتريات جديد» — the same protection the receive
          dialog gives, and the deliberate contrast with the no-confirm draft-line
          delete, DEC-PUR-005). The guard runs *after* the shared submit guidance,
          so an invalid form still reports its fields instead of opening a dialog.
        -->
        <form class="form" [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="requestConfirm()">
          @if (failureMessage(); as message) {
            <vf-banner tone="error" #failureBanner>{{ message }}</vf-banner>
          }

          <div class="date-field">
            <vf-form-field [label]="t.t('purchaseReturn.field.returnDate')" [required]="true">
              <vf-date-input [formControl]="form.controls.returnDate" />
            </vf-form-field>
          </div>

          <table class="vf-table">
            <thead>
              <tr>
                <th scope="col">{{ t.t('purchaseReturn.column.product') }}</th>
                <th scope="col" class="vf-num">{{ t.t('purchaseReturn.column.originalQuantity') }}</th>
                <th scope="col" class="vf-num">{{ t.t('purchaseReturn.column.returnable') }}</th>
                <th scope="col">{{ t.t('purchaseReturn.column.returnQuantity') }}</th>
              </tr>
            </thead>
            <tbody>
              @for (line of store.lines(); track line.purchaseLineItemId) {
                <tr>
                  <td>{{ line.productName }}</td>
                  <td class="vf-num">{{ format.decimal(line.quantity) }} {{ line.purchaseUnitName }}</td>
                  <td class="vf-num">{{ format.decimal(line.returnableQuantity) }} {{ line.purchaseUnitName }}</td>
                  <td>
                    <!--
                      The returnable remainder is enforced by the server (BR-PUR-016), the only place
                      that can see the committed returns. The client rejects only what is locally
                      wrong (a negative); there is deliberately no client-side ceiling pretending to
                      be the rule.
                    -->
                    @if (form.controls.quantities.controls[line.purchaseLineItemId]; as control) {
                      <vf-form-field [label]="line.productName" [messages]="quantityMessages">
                        <vf-number-input [formControl]="control" />
                      </vf-form-field>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>

          <vf-form-field [label]="t.t('purchaseReturn.field.notes')">
            <vf-textarea [formControl]="form.controls.notes" [rows]="3" />
          </vf-form-field>

          <div class="form-actions">
            <vf-button variant="primary" type="submit" [disabled]="saving()">
              {{ saveLabel() }}
            </vf-button>
            <a class="vf-link" [routerLink]="['/purchases', invoiceId()]">{{ t.t('purchaseReturn.cancel') }}</a>
          </div>
        </form>

        <vf-dialog [header]="t.t('purchaseReturn.save')" [(visible)]="confirmVisible">
          <p class="warn">{{ t.t('purchaseReturn.confirm') }}</p>

          <div dialogFooter>
            <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="confirmSave()">
              {{ saveLabel() }}
            </vf-button>
            <vf-button variant="quiet" [disabled]="saving()" (pressed)="confirmVisible.set(false)">
              {{ t.t('purchaseReturn.cancel') }}
            </vf-button>
          </div>
        </vf-dialog>
      }

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
    </div>
  `,
  styles: `
    @use '../../../shared/styles/numeric' as numeric;

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

    .form {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-4);
    }

    /* Instance layout only — the banner's own chrome is the shared component's (STD-UX-121). */
    .saved-banner {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .saved-banner a {
      color: inherit;
      font-weight: 600;
    }

    .date-field {
      max-inline-size: 20rem;
    }

    /*
      A short editable form table, not a data list: the UI Kit's <vf-table> is a scrollable PrimeNG
      datatable with state storage, built for lists rather than for a handful of rows each carrying
      an input. Styled here to the same table language rather than restructured — the deviation is
      reported, not hidden (TD-007).
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

    /* The two read-only quantity columns follow the one numeric standard (§6) —
       they followed none at all before, which is how an editable form table drifts
       away from every list in the product. The entry column is left alone: it holds
       a field, not a figure. */
    .vf-table {
      @include numeric.cells;
    }

    .vf-table tbody tr:last-child td {
      border-block-end: none;
    }

    /* The row already names the product in its first column; the field's own
       label serves assistive tech (STD-UX-093) without repeating visually.
       ::ng-deep because the label renders inside the child component — the
       same reach the UI Kit's own table styles use. */
    .vf-table td ::ng-deep .vf-field-label {
      position: absolute;
      inline-size: 1px;
      block-size: 1px;
      overflow: hidden;
      clip: rect(0 0 0 0);
      white-space: nowrap;
    }

    .form-actions {
      display: flex;
      align-items: center;
      gap: var(--vf-space-4);
    }

    /* The irreversibility sentence carries the dialog — same weight as the
       receive dialog's warning, so the two confirmations read alike. */
    .warn {
      margin: 0;
      color: var(--vf-text);
      font-weight: 600;
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
  private readonly focus = inject(ValidationFocusService);

  protected readonly invoiceId = signal('');

  /** Visibility of the commit-confirmation dialog (purchasing ui.md). */
  protected readonly confirmVisible = signal(false);

  // The documented rules only (STD-UX-021): the date is required by the
  // create contract; a quantity cell is empty-or-non-negative (zero skips the
  // line; the > 0 rule for a chosen line and the returnable ceiling are the
  // server's — VTF-PUR-017 / BR-PUR-016). Notes are free text.
  protected readonly form = new FormGroup({
    returnDate: new FormControl<string | null>(null, vfValidators.required),
    notes: new FormControl('', { nonNullable: true }),
    quantities: new FormRecord<FormControl<number | null>>({}),
  });

  // Zero is a valid "not returning this line" — the sentence must not demand > 0 (STD-UX-017).
  protected readonly quantityMessages: RuleMessageOverrides = { nonNegative: 'validation.nonNegative' };

  private readonly failureBanner = viewChild('failureBanner', { read: ElementRef });

  protected readonly saving = computed(() => this.store.submit().kind === 'saving');

  protected readonly savedNumber = computed(() => {
    const state = this.store.submit();
    return state.kind === 'saved' ? state.number : '';
  });

  protected readonly failureMessage = computed(() => {
    const state = this.store.submit();
    if (state.kind !== 'failed') {
      return null;
    }

    const message = this.t.t(state.failure.messageKey, state.failure.params);
    // The partial document state is stated, never left implicit (STD-UX-042).
    return state.draftCreated ? `${message} ${this.t.t('purchaseReturn.error.draftState')}` : message;
  });

  // A retryable conflict re-offers the same action as a retry (STD-UX-033).
  protected readonly saveLabel = computed(() => {
    if (this.saving()) {
      return this.t.t('purchaseReturn.saving');
    }

    const state = this.store.submit();
    return state.kind === 'failed' && state.failure.retryable
      ? this.t.t('errors.retry')
      : this.t.t('purchaseReturn.save');
  });

  // Screen-level outcomes for the polite live region (STD-UX-092): the
  // returnable-lines load, the in-flight commit, and the saved fact (its
  // banner is a status insertion, which alone is not reliably announced).
  // Rejections announce through their `role="alert"` surfaces.
  protected readonly announcement = computed(() => {
    if (this.store.submit().kind === 'saved') {
      return `${this.t.t('purchaseReturn.saved')} ${this.savedNumber()}`;
    }

    if (this.saving()) {
      return this.t.t('purchaseReturn.saving');
    }

    return this.store.linesLoading() ? this.t.t('purchaseReturn.loading') : '';
  });

  constructor() {
    // One quantity control per returnable line, rebuilt when the read lands.
    effect(() => {
      const lines = this.store.lines();
      const record = this.form.controls.quantities;
      for (const key of Object.keys(record.controls)) {
        record.removeControl(key);
      }

      for (const line of lines) {
        record.addControl(line.purchaseLineItemId, new FormControl<number | null>(null, nonNegative));
      }
    });

    // A rejection banner never survives the edit that addresses it (STD-UX-035).
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.store.submit().kind === 'failed') {
        this.store.reset();
      }
    });

    // An operation-level rejection has no field target: the banner itself
    // receives focus and is announced (STD-UX-071).
    effect(() => {
      if (this.store.submit().kind !== 'failed') {
        return;
      }

      const banner = this.failureBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.invoiceId.set(id);
    // The return date defaults to the clinic's today as the browser sees it; the business date
    // basis itself is the server's (BR-INV-059/060), which is why nothing is derived from it here.
    this.form.controls.returnDate.setValue(new Date().toISOString().slice(0, 10));
    if (id) {
      this.store.loadLines(id);
    }
  }

  /** Opens the irreversibility confirmation; the form is already valid when this runs. */
  protected requestConfirm(): void {
    this.confirmVisible.set(true);
  }

  /** The confirmed action: close the dialog, then perform the write. */
  protected confirmSave(): void {
    this.confirmVisible.set(false);
    this.save();
  }

  protected save(): void {
    const { returnDate, notes, quantities } = this.form.getRawValue();
    if (!returnDate) {
      return;
    }

    const chosen = new Map<string, number>();
    for (const [lineId, quantity] of Object.entries(quantities)) {
      chosen.set(lineId, quantity ?? 0);
    }

    this.store.save(this.invoiceId(), returnDate, notes.trim() === '' ? null : notes.trim(), chosen);
  }
}
