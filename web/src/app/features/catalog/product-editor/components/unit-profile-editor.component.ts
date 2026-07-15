import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { FormatService } from '../../../../core/i18n/format.service';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { inject } from '@angular/core';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../../shared/ui-kit/checkbox/vf-checkbox.component';
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
 */
@Component({
  selector: 'app-unit-profile-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    VfButtonComponent,
    VfCheckboxComponent,
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
            <vf-select
              class="row-unit"
              [label]="t.t('editor.units.unit')"
              [required]="true"
              [placeholder]="t.t('editor.select.placeholder')"
              [filterable]="true"
              [optionList]="unitSelectOptions()"
              [value]="unitIdControl(row).value"
              [error]="showErrors() && unitIdControl(row).invalid ? t.t('editor.required') : null"
              (valueChange)="unitIdControl(row).setValue($event)"
            />
            <vf-number-input
              class="row-conversion"
              [label]="t.t('editor.units.conversion')"
              [placeholder]="t.t('editor.units.conversionHint')"
              [min]="0"
              [formControl]="conversionControl(row)"
            />
          </div>

          <div class="row-flags">
            <vf-checkbox [checked]="purchaseControl(row).value" (toggled)="toggle(purchaseControl(row))">
              {{ t.t('editor.units.isPurchase') }}
            </vf-checkbox>
            <vf-checkbox [checked]="saleControl(row).value" (toggled)="toggle(saleControl(row))">
              {{ t.t('editor.units.isSale') }}
            </vf-checkbox>
          </div>

          <div class="row-extra">
            <vf-text-input [label]="t.t('editor.units.barcode')" [formControl]="barcodeControl(row)" />
            @if (saleControl(row).value) {
              @if (priceEditable()) {
                <vf-number-input
                  [label]="t.t('editor.units.price')"
                  [min]="0"
                  [formControl]="priceControl(row)"
                />
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
  readonly showErrors = input(false);
  /** Edit mode shows the price read-only; it is never mutated (DEC-CAT-031). */
  readonly priceEditable = input(true);
  /** Currency for the read-only price; the persisted product's own currency. */
  readonly currency = input('EGP');
  readonly addRow = output<void>();
  readonly removeRow = output<number>();

  protected priceDisplay(row: UnitRowForm): string {
    const amount = row.controls.sellingPrice.value;
    return amount === null ? '—' : this.format.money(amount, this.currency());
  }

  protected unitSelectOptions(): readonly VfSelectOption<string>[] {
    return this.unitOptions().map((option) => ({ label: option.name, value: option.id }));
  }

  protected unitIdControl(row: UnitRowForm): FormControl<string | null> {
    return row.controls.unitId;
  }

  protected conversionControl(row: UnitRowForm): FormControl<number | null> {
    return row.controls.quantityInNextUnit;
  }

  protected purchaseControl(row: UnitRowForm): FormControl<boolean> {
    return row.controls.isPurchaseUnit;
  }

  protected saleControl(row: UnitRowForm): FormControl<boolean> {
    return row.controls.isSaleUnit;
  }

  protected barcodeControl(row: UnitRowForm): FormControl<string> {
    return row.controls.barcode;
  }

  protected priceControl(row: UnitRowForm): FormControl<number | null> {
    return row.controls.sellingPrice;
  }

  protected toggle(control: FormControl<boolean>): void {
    control.setValue(!control.value);
  }
}
