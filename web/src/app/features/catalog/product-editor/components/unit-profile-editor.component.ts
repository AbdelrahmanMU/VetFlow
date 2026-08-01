import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfFormFieldComponent } from '../../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfNumberInputComponent } from '../../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextInputComponent } from '../../../../shared/ui-kit/input/vf-text-input.component';
import { VfSelectComponent, VfSelectOption } from '../../../../shared/ui-kit/select/vf-select.component';
import { LookupOption } from '../product-editor.models';
import { UnitRowForm } from '../product-editor.forms';

/**
 * The unit-profile rows editor (screen S4 embedded in S3, catalog ui.md §5/§6):
 * each row is a unit with its user-entered conversion factor (never derived —
 * DEC-CAT-009), roles, optional barcode and manual price (BR-CAT-016/018/024/025).
 * A presentation component — it mutates the passed reactive controls and emits
 * add/remove; it injects no data service (STD-FE-010).
 *
 * Validation-foundation adoption (validation-and-guidance.md): every row field
 * renders through `vf-form-field` (STD-UX-120) — required unit, positive
 * conversion factor with its hint (STD-UX-013), barcode length — so rows keep
 * the three moments and the aria wiring like any other field. The role
 * checkboxes bind through the repaired `vf-checkbox` CVA; when a cross-row
 * role rule is violated (BR-CAT-024/025) the parent passes the flags so the
 * relevant checkboxes carry `aria-invalid` and point at the rule's message
 * (STD-UX-090/091).
 */
@Component({
  selector: 'app-unit-profile-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    VfButtonComponent,
    VfCheckboxComponent,
    VfFormFieldComponent,
    VfNumberInputComponent,
    VfTextInputComponent,
    VfSelectComponent,
  ],
  template: `
    <div class="editor">
      @if (rows().length === 0) {
        <p class="empty">{{ t.t('editor.units.empty') }}</p>
      }

      @for (row of rows(); track row; let index = $index) {
        <div class="row">
          <div class="row-main">
            <vf-form-field class="row-unit" [label]="t.t('editor.units.unit')" [required]="true">
              <vf-select
                [formControl]="row.controls.unitId"
                [placeholder]="t.t('editor.select.placeholder')"
                [filterable]="true"
                [optionList]="unitSelectOptions()"
              />
            </vf-form-field>
            <vf-form-field
              class="row-conversion"
              [label]="t.t('editor.units.conversion')"
              [hint]="t.t('editor.units.conversionHint')"
            >
              <vf-number-input [formControl]="row.controls.quantityInNextUnit" [min]="0" />
            </vf-form-field>
          </div>

          <div class="row-flags">
            <vf-checkbox
              [formControl]="row.controls.isPurchaseUnit"
              [invalid]="purchaseRuleError()"
              [describedBy]="purchaseRuleError() ? rulesMessageId() : null"
            >
              {{ t.t('editor.units.isPurchase') }}
            </vf-checkbox>
            <vf-checkbox
              [formControl]="row.controls.isSaleUnit"
              [invalid]="saleRuleError()"
              [describedBy]="saleRuleError() ? rulesMessageId() : null"
            >
              {{ t.t('editor.units.isSale') }}
            </vf-checkbox>
          </div>

          <div class="row-extra">
            <vf-form-field [label]="t.t('editor.units.barcode')">
              <vf-text-input [formControl]="row.controls.barcode" />
            </vf-form-field>
            @if (row.controls.isSaleUnit.value) {
              @if (priceEditable()) {
                <vf-form-field [label]="t.t('editor.units.price')">
                  <vf-number-input [formControl]="row.controls.sellingPrice" [min]="0" />
                </vf-form-field>
              } @else {
                <div class="readonly-field">
                  <span class="readonly-caption">{{ t.t('editor.units.priceReadonly') }}</span>
                  <span class="readonly-value vf-num">{{ priceDisplay(row) }}</span>
                </div>
              }
            }
            <vf-button variant="quiet" icon="pi-trash" (pressed)="removeRow.emit(index)">
              {{ t.t('editor.units.remove') }}
            </vf-button>
          </div>
        </div>
      }

      <vf-button variant="secondary" icon="pi-plus" (pressed)="addRow.emit()">
        {{ t.t('editor.units.add') }}
      </vf-button>
    </div>
  `,
  styles: `
    .editor {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
    }

    .empty {
      margin: 0;
      color: var(--vf-text-secondary);
    }

    .row {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
      padding: var(--vf-space-3);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius-small);
      background: var(--vf-bg);
    }

    .row-main {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: var(--vf-space-3);
    }

    .row-flags {
      display: flex;
      gap: var(--vf-space-4);
    }

    .row-extra {
      display: grid;
      grid-template-columns: 2fr 1fr auto;
      align-items: end;
      gap: var(--vf-space-3);
    }

    .readonly-field {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
      padding-block-end: 0.5rem;
    }

    .readonly-caption {
      font-size: var(--vf-text-secondary-size);
      color: var(--vf-text-secondary);
      font-weight: 500;
    }

    .readonly-value {
      color: var(--vf-text);
      font-size: var(--vf-text-body);
    }

    @media (max-width: 768px) {
      .row-main,
      .row-extra {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class UnitProfileEditorComponent {
  protected readonly t = inject(TranslationService);
  private readonly format = inject(FormatService);

  readonly rows = input.required<readonly UnitRowForm[]>();
  readonly unitOptions = input.required<readonly LookupOption[]>();
  /** Edit mode shows the price read-only; it is never mutated (DEC-CAT-031). */
  readonly priceEditable = input(true);
  /** Currency for the read-only price; the persisted product's own currency. */
  readonly currency = input('EGP');
  /** Cross-row role rules, revealed by the parent at moment 3 (BR-CAT-024/025). */
  readonly purchaseRuleError = input(false);
  readonly saleRuleError = input(false);
  /** `id` of the parent's units-rule message area, for `aria-describedby` (STD-UX-091). */
  readonly rulesMessageId = input<string | null>(null);
  readonly addRow = output<void>();
  readonly removeRow = output<number>();

  protected priceDisplay(row: UnitRowForm): string {
    const amount = row.controls.sellingPrice.value;
    return amount === null ? '—' : this.format.money(amount, this.currency());
  }

  protected unitSelectOptions(): readonly VfSelectOption<string>[] {
    return this.unitOptions().map((option) => ({ label: option.name, value: option.id }));
  }
}
