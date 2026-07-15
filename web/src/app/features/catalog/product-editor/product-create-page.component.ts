import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, startWith } from 'rxjs/operators';

import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { VfSelectComponent, VfSelectOption } from '../../../shared/ui-kit/select/vf-select.component';
import { DuplicateWarningDialogComponent } from './components/duplicate-warning-dialog.component';
import { UnitProfileEditorComponent } from './components/unit-profile-editor.component';
import { ProductEditorApiService } from './product-editor-api.service';
import { CreateProductPayload, LookupOption, PossibleDuplicate } from './product-editor.models';
import { UnitRowForm, newUnitRow } from './product-editor.forms';

function productFormValidator(control: AbstractControl): ValidationErrors | null {
  const group = control as FormGroup;
  const errors: ValidationErrors = {};

  if (group.get('hasOpenExpiration')?.value) {
    const days = group.get('openExpirationPeriodDays')?.value as number | null;
    if (days === null || days <= 0) {
      errors['openExpiration'] = true;
    }
  }

  const units = group.get('units') as FormArray<UnitRowForm>;
  if (units.length === 0) {
    errors['unitsEmpty'] = true;
  }
  if (!units.controls.some((row) => row.controls.isPurchaseUnit.value)) {
    errors['noPurchase'] = true;
  }
  if (!units.controls.some((row) => row.controls.isSaleUnit.value)) {
    errors['noSale'] = true;
  }

  return Object.keys(errors).length > 0 ? errors : null;
}

/**
 * Screen S3 — محرر المنتج (create) (catalog ui.md §5, WF-CAT-001): the mandatory
 * minimum (BR-CAT-009), identity/classification/capabilities, the unit profile
 * (DEC-CAT-007/020), optional prices and notes. On save it runs the
 * possible-duplicate advisory (DEC-CAT-027) and warns without ever blocking
 * (BR-CAT-042 / DEC-CAT-018). Typed reactive forms throughout (STD-FE-016).
 */
@Component({
  selector: 'app-product-create-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProductEditorApiService],
  imports: [
    ReactiveFormsModule,
    VfButtonComponent,
    VfCheckboxComponent,
    VfNumberInputComponent,
    VfTextInputComponent,
    VfTextareaComponent,
    VfSelectComponent,
    UnitProfileEditorComponent,
    DuplicateWarningDialogComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('editor.create.title') }}</h1>
      </header>

      @if (banner()) {
        <p class="banner" role="alert">{{ t.t('editor.error') }}</p>
      }

      <section class="card">
        <h2 class="card-title">{{ t.t('editor.section.identity') }}</h2>
        <div class="grid">
          <vf-text-input
            [label]="t.t('editor.field.arabicName')"
            [required]="true"
            [formControl]="form.controls.arabicName"
            [error]="errorFor(form.controls.arabicName)"
          />
          <vf-text-input [label]="t.t('editor.field.englishName')" [formControl]="form.controls.englishName" />
          <vf-text-input [label]="t.t('editor.field.size')" [formControl]="form.controls.size" />
          <vf-text-input [label]="t.t('editor.field.concentration')" [formControl]="form.controls.concentration" />
        </div>
      </section>

      <section class="card">
        <h2 class="card-title">{{ t.t('editor.section.classification') }}</h2>
        <div class="grid">
          <vf-select
            [label]="t.t('editor.field.category')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [filterable]="true"
            [optionList]="lookupOptions(categoryOptions())"
            [value]="form.controls.categoryId.value"
            [error]="errorFor(form.controls.categoryId)"
            (valueChange)="form.controls.categoryId.setValue($event)"
          />
          <vf-select
            [label]="t.t('editor.field.manufacturer')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [filterable]="true"
            [optionList]="lookupOptions(manufacturerOptions())"
            [value]="form.controls.manufacturerId.value"
            [error]="errorFor(form.controls.manufacturerId)"
            (valueChange)="form.controls.manufacturerId.setValue($event)"
          />
          <vf-select
            [label]="t.t('editor.field.nature')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [optionList]="lookupOptions(natureOptions())"
            [value]="form.controls.natureId.value"
            [error]="errorFor(form.controls.natureId)"
            (valueChange)="form.controls.natureId.setValue($event)"
          />
        </div>
      </section>

      <section class="card">
        <h2 class="card-title">{{ t.t('editor.section.capabilities') }}</h2>
        <div class="capabilities">
          <vf-checkbox [checked]="form.controls.isSplittable.value" (toggled)="toggle(form.controls.isSplittable)">
            {{ t.t('editor.field.splittable') }}
          </vf-checkbox>
          <vf-checkbox [checked]="form.controls.isRefrigerated.value" (toggled)="toggle(form.controls.isRefrigerated)">
            {{ t.t('editor.field.refrigerated') }}
          </vf-checkbox>
          <vf-checkbox [checked]="form.controls.hasExpiration.value" (toggled)="toggle(form.controls.hasExpiration)">
            {{ t.t('editor.field.hasExpiration') }}
          </vf-checkbox>
          <vf-checkbox [checked]="form.controls.hasOpenExpiration.value" (toggled)="toggleOpenExpiration()">
            {{ t.t('editor.field.hasOpenExpiration') }}
          </vf-checkbox>
        </div>
        @if (form.controls.hasOpenExpiration.value) {
          <div class="open-expiration">
            <vf-number-input
              [label]="t.t('editor.field.openExpirationDays')"
              [required]="true"
              [min]="1"
              [formControl]="form.controls.openExpirationPeriodDays"
              [error]="submitted() && form.hasError('openExpiration') ? t.t('editor.required') : null"
            />
          </div>
        }
      </section>

      <section class="card">
        <h2 class="card-title">{{ t.t('editor.section.units') }}</h2>
        <p class="hint">{{ t.t('editor.units.storageHint') }}</p>
        <app-unit-profile-editor
          [rows]="unitRows()"
          [unitOptions]="unitOptions()"
          [showErrors]="submitted()"
          (addRow)="addUnitRow()"
          (removeRow)="removeUnitRow($event)"
        />
        @if (submitted() && (form.hasError('noPurchase') || form.hasError('noSale') || form.hasError('unitsEmpty'))) {
          <p class="banner" role="alert">{{ t.t('editor.units.empty') }}</p>
        }
        <div class="grid roles">
          <vf-select
            [label]="t.t('editor.units.storageUnit')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [optionList]="profileUnitOptions()"
            [value]="form.controls.storageUnitId.value"
            [error]="errorFor(form.controls.storageUnitId)"
            (valueChange)="form.controls.storageUnitId.setValue($event)"
          />
          <vf-select
            [label]="t.t('editor.units.defaultSale')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [optionList]="profileUnitOptions()"
            [value]="form.controls.defaultSaleUnitId.value"
            [error]="errorFor(form.controls.defaultSaleUnitId)"
            (valueChange)="form.controls.defaultSaleUnitId.setValue($event)"
          />
          <vf-select
            [label]="t.t('editor.units.defaultPurchase')"
            [required]="true"
            [placeholder]="t.t('editor.select.placeholder')"
            [optionList]="profileUnitOptions()"
            [value]="form.controls.defaultPurchaseUnitId.value"
            [error]="errorFor(form.controls.defaultPurchaseUnitId)"
            (valueChange)="form.controls.defaultPurchaseUnitId.setValue($event)"
          />
        </div>
      </section>

      <section class="card">
        <h2 class="card-title">{{ t.t('editor.section.pricingNote') }}</h2>
        <p class="hint">{{ t.t('editor.priceHint') }}</p>
        <vf-textarea [label]="t.t('editor.field.notes')" [rows]="3" [formControl]="form.controls.internalNotes" />
      </section>

      <footer class="actions">
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="submit()">
          {{ saving() ? t.t('editor.saving') : t.t('editor.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="cancel()">
          {{ t.t('editor.cancel') }}
        </vf-button>
      </footer>

      <app-duplicate-warning-dialog
        [(visible)]="dialogVisible"
        [duplicates]="duplicates()"
        [enteredName]="form.controls.arabicName.value"
        [enteredSize]="nullable(form.controls.size.value)"
        [enteredConcentration]="nullable(form.controls.concentration.value)"
        (openExisting)="openExisting($event)"
        (continueSaving)="continueAfterWarning()"
      />
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

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
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

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
      gap: var(--vf-space-3);
    }

    .roles {
      margin-block-start: var(--vf-space-4);
    }

    .capabilities {
      display: flex;
      flex-wrap: wrap;
      gap: var(--vf-space-4);
    }

    .open-expiration {
      margin-block-start: var(--vf-space-3);
      max-inline-size: 18rem;
    }

    .hint {
      margin: 0 0 var(--vf-space-3);
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
    }

    .banner {
      margin: 0;
      padding: var(--vf-space-3);
      border-radius: var(--vf-radius-small);
      background: var(--vf-danger-soft, #fbeae8);
      color: var(--vf-danger, #b42318);
      font-weight: 500;
    }

    .actions {
      display: flex;
      gap: var(--vf-space-2);
      position: sticky;
      inset-block-end: 0;
      padding-block: var(--vf-space-3);
      background: var(--vf-bg);
    }
  `,
})
export class ProductCreatePageComponent {
  protected readonly t = inject(TranslationService);
  private readonly api = inject(ProductEditorApiService);
  private readonly router = inject(Router);

  readonly form = new FormGroup(
    {
      arabicName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      englishName: new FormControl('', { nonNullable: true }),
      size: new FormControl('', { nonNullable: true }),
      concentration: new FormControl('', { nonNullable: true }),
      categoryId: new FormControl<string | null>(null, [Validators.required]),
      manufacturerId: new FormControl<string | null>(null, [Validators.required]),
      natureId: new FormControl<string | null>(null, [Validators.required]),
      isSplittable: new FormControl(false, { nonNullable: true }),
      isRefrigerated: new FormControl(false, { nonNullable: true }),
      hasExpiration: new FormControl(false, { nonNullable: true }),
      hasOpenExpiration: new FormControl(false, { nonNullable: true }),
      openExpirationPeriodDays: new FormControl<number | null>(null),
      internalNotes: new FormControl('', { nonNullable: true }),
      units: new FormArray<UnitRowForm>([newUnitRow(), newUnitRow()]),
      storageUnitId: new FormControl<string | null>(null, [Validators.required]),
      defaultSaleUnitId: new FormControl<string | null>(null, [Validators.required]),
      defaultPurchaseUnitId: new FormControl<string | null>(null, [Validators.required]),
    },
    { validators: [productFormValidator] },
  );

  readonly submitted = signal(false);
  readonly saving = signal(false);
  readonly banner = signal(false);
  readonly duplicates = signal<readonly PossibleDuplicate[]>([]);
  readonly dialogVisible = signal(false);
  protected readonly unitRows = signal<readonly UnitRowForm[]>(this.form.controls.units.controls);

  protected readonly categoryOptions = this.lookupSignal(() => this.api.categoryOptions());
  protected readonly manufacturerOptions = this.lookupSignal(() => this.api.manufacturerOptions());
  protected readonly natureOptions = this.lookupSignal(() => this.api.natureOptions());
  protected readonly unitOptions = this.lookupSignal(() => this.api.unitOptions());

  private readonly unitsValue = toSignal(
    this.form.controls.units.valueChanges.pipe(startWith(this.form.controls.units.getRawValue())),
    { initialValue: this.form.controls.units.getRawValue() },
  );

  /** The units already chosen in the profile — the pool for storage/default selects. */
  protected readonly profileUnitOptions = computed<readonly VfSelectOption<string>[]>(() => {
    const byId = new Map(this.unitOptions().map((option) => [option.id, option.name]));
    const seen = new Set<string>();
    const options: VfSelectOption<string>[] = [];
    for (const row of this.unitsValue()) {
      const id = row.unitId;
      if (id && !seen.has(id) && byId.has(id)) {
        seen.add(id);
        options.push({ label: byId.get(id)!, value: id });
      }
    }

    return options;
  });

  protected lookupOptions(options: readonly LookupOption[]): readonly VfSelectOption<string>[] {
    return options.map((option) => ({ label: option.name, value: option.id }));
  }

  protected errorFor(control: AbstractControl): string | null {
    return (this.submitted() || control.touched) && control.invalid ? this.t.t('editor.required') : null;
  }

  protected nullable(value: string): string | null {
    return value.trim() === '' ? null : value.trim();
  }

  protected toggle(control: FormControl<boolean>): void {
    control.setValue(!control.value);
  }

  protected toggleOpenExpiration(): void {
    const next = !this.form.controls.hasOpenExpiration.value;
    this.form.controls.hasOpenExpiration.setValue(next);
    if (!next) {
      this.form.controls.openExpirationPeriodDays.setValue(null);
    }
  }

  protected addUnitRow(): void {
    this.form.controls.units.push(newUnitRow());
    this.unitRows.set([...this.form.controls.units.controls]);
  }

  protected removeUnitRow(index: number): void {
    this.form.controls.units.removeAt(index);
    this.unitRows.set([...this.form.controls.units.controls]);
  }

  submit(): void {
    this.submitted.set(true);
    this.banner.set(false);
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    const payload = this.buildPayload();
    this.saving.set(true);
    this.api.possibleDuplicates(payload.arabicName, payload.manufacturerId).subscribe({
      next: (result) => {
        this.saving.set(false);
        if (result.items.length > 0) {
          this.duplicates.set(result.items);
          this.dialogVisible.set(true);
        } else {
          this.create(payload);
        }
      },
      error: () => {
        // The duplicate check is advisory (BR-CAT-042); a failure must never block saving.
        this.saving.set(false);
        this.create(payload);
      },
    });
  }

  protected continueAfterWarning(): void {
    this.dialogVisible.set(false);
    this.create(this.buildPayload());
  }

  protected openExisting(id: string): void {
    this.dialogVisible.set(false);
    void this.router.navigate(['/catalog/products', id]);
  }

  protected cancel(): void {
    void this.router.navigate(['/catalog/products']);
  }

  private create(payload: CreateProductPayload): void {
    this.saving.set(true);
    this.banner.set(false);
    this.api.create(payload).subscribe({
      next: (created) => {
        this.saving.set(false);
        void this.router.navigate(['/catalog/products', created.id]);
      },
      error: () => {
        this.saving.set(false);
        this.banner.set(true);
      },
    });
  }

  private buildPayload(): CreateProductPayload {
    const raw = this.form.getRawValue();
    return {
      arabicName: raw.arabicName.trim(),
      englishName: this.nullable(raw.englishName),
      size: this.nullable(raw.size),
      concentration: this.nullable(raw.concentration),
      categoryId: raw.categoryId ?? '',
      manufacturerId: raw.manufacturerId ?? '',
      natureId: raw.natureId ?? '',
      isSplittable: raw.isSplittable,
      isRefrigerated: raw.isRefrigerated,
      hasExpiration: raw.hasExpiration,
      hasOpenExpiration: raw.hasOpenExpiration,
      openExpirationPeriodDays: raw.hasOpenExpiration ? raw.openExpirationPeriodDays : null,
      internalNotes: this.nullable(raw.internalNotes),
      units: raw.units.map((unit, index) => ({
        unitId: unit.unitId ?? '',
        position: index,
        quantityInNextUnit: unit.quantityInNextUnit,
        isPurchaseUnit: unit.isPurchaseUnit,
        isSaleUnit: unit.isSaleUnit,
        barcode: this.nullable(unit.barcode),
        sellingPrice: unit.isSaleUnit ? unit.sellingPrice : null,
      })),
      storageUnitId: raw.storageUnitId ?? '',
      defaultSaleUnitId: raw.defaultSaleUnitId ?? '',
      defaultPurchaseUnitId: raw.defaultPurchaseUnitId ?? '',
    };
  }

  private lookupSignal(load: () => ReturnType<ProductEditorApiService['categoryOptions']>) {
    return toSignal(
      load().pipe(
        map((result) => result.items),
        catchError(() => of<readonly LookupOption[]>([])),
      ),
      { initialValue: [] as readonly LookupOption[] },
    );
  }
}
