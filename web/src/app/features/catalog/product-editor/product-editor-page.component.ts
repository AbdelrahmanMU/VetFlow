import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, startWith } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfCheckboxComponent } from '../../../shared/ui-kit/checkbox/vf-checkbox.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfNumberInputComponent } from '../../../shared/ui-kit/input/vf-number-input.component';
import { VfTextInputComponent } from '../../../shared/ui-kit/input/vf-text-input.component';
import { VfTextareaComponent } from '../../../shared/ui-kit/input/vf-textarea.component';
import { VfSelectComponent, VfSelectOption } from '../../../shared/ui-kit/select/vf-select.component';
import { DuplicateWarningDialogComponent } from './components/duplicate-warning-dialog.component';
import { UnitProfileEditorComponent } from './components/unit-profile-editor.component';
import { ProductEditorApiService } from './product-editor-api.service';
import {
  CreateProductPayload,
  EditProduct,
  LookupOption,
  PossibleDuplicate,
  UpdateProductPayload,
} from './product-editor.models';
import { ProductForm, UnitRowForm, buildProductForm, newUnitRow, unitRowFrom } from './product-editor.forms';

export type EditorMode = 'create' | 'edit';
type EditLoadState = 'loading' | 'ready' | 'notFound' | 'error';

/**
 * Screen S3 — محرر المنتج, the unified create/edit editor (catalog ui.md §5,
 * WF-CAT-001, DEC-CAT-031). One component, one template: the mode (route data)
 * decides the title, whether the possible-duplicate advisory runs (create only),
 * whether prices are editable (create) or read-only (edit — never mutated), and
 * whether it saves via POST or PUT. Typed reactive forms throughout (STD-FE-016).
 */
@Component({
  selector: 'app-product-editor-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProductEditorApiService],
  imports: [
    ReactiveFormsModule,
    VfButtonComponent,
    VfCheckboxComponent,
    VfEmptyStateComponent,
    VfNumberInputComponent,
    VfTextInputComponent,
    VfTextareaComponent,
    VfSelectComponent,
    UnitProfileEditorComponent,
    DuplicateWarningDialogComponent,
  ],
  template: `
    <div class="page">
      @switch (loadState()) {
        @case ('loading') {
          <div class="state" role="status">{{ t.t('editor.loading') }}</div>
        }
        @case ('notFound') {
          <vf-empty-state
            icon="pi-inbox"
            [title]="t.t('productDetails.notFound.title')"
            [body]="t.t('productDetails.notFound.body')"
          >
            <vf-button variant="secondary" icon="pi-arrow-right" (pressed)="cancel()">
              {{ t.t('productDetails.back') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-circle"
            [title]="t.t('productDetails.error.title')"
            [body]="t.t('productDetails.error.body')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="reload()">
              {{ t.t('productDetails.error.retry') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('ready') {
          <header class="page-header">
            <h1 class="page-title">{{ pageTitle() }}</h1>
            @if (mode() === 'edit' && loaded(); as product) {
              <p class="internal-code vf-num">{{ t.t('editor.internalCode') }}: {{ product.internalCode }}</p>
            }
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
              [priceEditable]="mode() === 'create'"
              [currency]="currency()"
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
              {{ saving() ? t.t('editor.saving') : saveLabel() }}
            </vf-button>
            <vf-button variant="quiet" [disabled]="saving()" (pressed)="cancel()">
              {{ t.t('editor.cancel') }}
            </vf-button>
          </footer>

          @if (mode() === 'create') {
            <app-duplicate-warning-dialog
              [(visible)]="dialogVisible"
              [duplicates]="duplicates()"
              [enteredName]="form.controls.arabicName.value"
              [enteredSize]="nullable(form.controls.size.value)"
              [enteredConcentration]="nullable(form.controls.concentration.value)"
              (openExisting)="openExisting($event)"
              (continueSaving)="continueAfterWarning()"
            />
          }
        }
      }
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

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .internal-code {
      margin: var(--vf-space-1) 0 0;
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
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
export class ProductEditorPageComponent implements OnInit {
  protected readonly t = inject(TranslationService);
  private readonly api = inject(ProductEditorApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Route data via withComponentInputBinding(): 'create' or 'edit'. */
  readonly mode = input.required<EditorMode>();
  /** Route parameter via withComponentInputBinding(); present in edit mode. */
  readonly id = input<string>();

  readonly form: ProductForm = buildProductForm();

  readonly submitted = signal(false);
  readonly saving = signal(false);
  readonly banner = signal(false);
  readonly duplicates = signal<readonly PossibleDuplicate[]>([]);
  readonly dialogVisible = signal(false);
  readonly loaded = signal<EditProduct | null>(null);
  readonly loadState = signal<EditLoadState>('ready');
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

  protected readonly pageTitle = computed(() =>
    this.mode() === 'edit' ? (this.loaded()?.arabicName ?? this.t.t('editor.edit.title')) : this.t.t('editor.create.title'),
  );

  protected readonly saveLabel = computed(() =>
    this.mode() === 'edit' ? this.t.t('editor.update') : this.t.t('editor.save'),
  );

  /** The persisted product's currency, for the read-only price display (edit). */
  protected readonly currency = computed(
    () => this.loaded()?.units.find((unit) => unit.sellingPrice)?.sellingPrice?.currency ?? 'EGP',
  );

  ngOnInit(): void {
    if (this.mode() === 'edit') {
      this.reload();
    }
  }

  protected reload(): void {
    const id = this.id();
    if (!id) {
      this.loadState.set('error');
      return;
    }

    this.loadState.set('loading');
    this.api
      .load(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          this.prefill(product);
          this.loaded.set(product);
          this.loadState.set('ready');
        },
        error: (error: unknown) => {
          this.loadState.set(error instanceof ApiError && error.status === 404 ? 'notFound' : 'error');
        },
      });
  }

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

    if (this.mode() === 'edit') {
      this.update();
      return;
    }

    const payload = this.buildCreatePayload();
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
    this.create(this.buildCreatePayload());
  }

  protected openExisting(id: string): void {
    this.dialogVisible.set(false);
    void this.router.navigate(['/catalog/products', id]);
  }

  protected cancel(): void {
    const id = this.id();
    if (this.mode() === 'edit' && id) {
      void this.router.navigate(['/catalog/products', id]);
      return;
    }

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

  private update(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    const payload = this.buildUpdatePayload();
    this.saving.set(true);
    this.banner.set(false);
    this.api.update(id, payload).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/catalog/products', id]);
      },
      error: () => {
        this.saving.set(false);
        this.banner.set(true);
      },
    });
  }

  private prefill(product: EditProduct): void {
    const controls = this.form.controls;
    controls.arabicName.setValue(product.arabicName);
    controls.englishName.setValue(product.englishName ?? '');
    controls.size.setValue(product.size ?? '');
    controls.concentration.setValue(product.concentration ?? '');
    controls.categoryId.setValue(product.categoryId);
    controls.manufacturerId.setValue(product.manufacturerId);
    controls.natureId.setValue(product.natureId);
    controls.isSplittable.setValue(product.isSplittable);
    controls.isRefrigerated.setValue(product.isRefrigerated);
    controls.hasExpiration.setValue(product.hasExpiration);
    controls.hasOpenExpiration.setValue(product.hasOpenExpiration);
    controls.openExpirationPeriodDays.setValue(product.openExpirationPeriodDays);
    controls.internalNotes.setValue(product.internalNotes ?? '');

    const orderedUnits = [...product.units].sort((a, b) => a.position - b.position);
    controls.units.clear();
    for (const unit of orderedUnits) {
      controls.units.push(unitRowFrom(unit));
    }
    this.unitRows.set([...controls.units.controls]);

    // Roles reference the unit ids, so the rows must be in place first.
    controls.storageUnitId.setValue(orderedUnits.find((unit) => unit.isStorageUnit)?.unitId ?? null);
    controls.defaultSaleUnitId.setValue(orderedUnits.find((unit) => unit.isDefaultSaleUnit)?.unitId ?? null);
    controls.defaultPurchaseUnitId.setValue(orderedUnits.find((unit) => unit.isDefaultPurchaseUnit)?.unitId ?? null);
  }

  /** The scalar fields shared by create and edit (no per-unit price). */
  private buildScalars() {
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
      storageUnitId: raw.storageUnitId ?? '',
      defaultSaleUnitId: raw.defaultSaleUnitId ?? '',
      defaultPurchaseUnitId: raw.defaultPurchaseUnitId ?? '',
    };
  }

  private buildCreatePayload(): CreateProductPayload {
    const raw = this.form.getRawValue();
    return {
      ...this.buildScalars(),
      units: raw.units.map((unit, index) => ({
        unitId: unit.unitId ?? '',
        position: index,
        quantityInNextUnit: unit.quantityInNextUnit,
        isPurchaseUnit: unit.isPurchaseUnit,
        isSaleUnit: unit.isSaleUnit,
        barcode: this.nullable(unit.barcode),
        sellingPrice: unit.isSaleUnit ? unit.sellingPrice : null,
      })),
    };
  }

  private buildUpdatePayload(): UpdateProductPayload {
    const raw = this.form.getRawValue();
    return {
      ...this.buildScalars(),
      units: raw.units.map((unit, index) => ({
        unitId: unit.unitId ?? '',
        position: index,
        quantityInNextUnit: unit.quantityInNextUnit,
        isPurchaseUnit: unit.isPurchaseUnit,
        isSaleUnit: unit.isSaleUnit,
        barcode: this.nullable(unit.barcode),
      })),
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
