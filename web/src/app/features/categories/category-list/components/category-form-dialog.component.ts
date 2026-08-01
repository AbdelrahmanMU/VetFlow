import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { ClassifiedFailure } from '../../../../core/validation/api-error-mapper';
import { projectServerFieldErrors } from '../../../../core/validation/server-errors';
import { SubmitGuidanceDirective } from '../../../../core/validation/submit-guidance.directive';
import { ValidationFocusService } from '../../../../core/validation/validation-focus.service';
import { RuleMessageOverrides } from '../../../../core/validation/validation-messages';
import { vfValidators } from '../../../../core/validation/validators';
import { VfBannerComponent } from '../../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfFormFieldComponent } from '../../../../shared/ui-kit/form-field/vf-form-field.component';
import { VfTextInputComponent } from '../../../../shared/ui-kit/input/vf-text-input.component';

export type CategoryDialogMode = 'create' | 'rename';

/**
 * Create/rename dialog (categories ui.md): one Arabic-name field (REQ-CTG-002/003)
 * behind the shared VfDialog + VfFormField. A presentation component — the page
 * owns the write and hands the classified failure in.
 *
 * Phase 1 proof-of-concept of the validation foundation
 * (validation-and-guidance.md): the field runs the three moments through
 * `vf-form-field` (required + max-length each with their own sentence,
 * STD-UX-017); a server `VTF-VAL-001` failure is projected back onto the
 * name field (STD-UX-019 — the duplicate-name rule, BR-CTG-003) and clears
 * on the next edit; any other failure renders as a focusable operation
 * message inside the dialog (STD-UX-070/080), never behind it (STD-UX-082).
 * Opening the dialog starts a fresh cycle.
 */
@Component({
  selector: 'app-category-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SubmitGuidanceDirective,
    VfBannerComponent,
    VfDialogComponent,
    VfFormFieldComponent,
    VfTextInputComponent,
    VfButtonComponent,
  ],
  template: `
    <vf-dialog [header]="title()" [(visible)]="visible">
      <form [formGroup]="form" [vfSubmitGuide]="form" (validSubmit)="emitSave()">
        @if (operationError(); as message) {
          <vf-banner tone="error" #operationBanner>{{ message }}</vf-banner>
        }
        <vf-form-field [label]="t.t('categories.dialog.name')" [required]="true" [messages]="nameMessages">
          <vf-text-input [formControl]="form.controls.name" />
        </vf-form-field>
      </form>
      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onSave()">
          {{ saving() ? t.t('categories.dialog.saving') : t.t('categories.dialog.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('categories.dialog.cancel') }}
        </vf-button>
      </div>
    </vf-dialog>
  `,
  styles: `
    /* Instance spacing only — the banner's own chrome is the shared component's (STD-UX-121). */
    vf-banner {
      margin-block-end: var(--vf-space-3);
    }
  `,
})
export class CategoryFormDialogComponent {
  protected readonly t = inject(TranslationService);
  // UI-infrastructure services (i18n, focus) — not data access (STD-FE-010).
  private readonly focus = inject(ValidationFocusService);

  readonly visible = model(false);
  readonly mode = input<CategoryDialogMode>('create');
  readonly initialName = input('');
  readonly saving = input(false);
  /** The page's classified save failure (ApiErrorMapper output) — null while clean. */
  readonly serverFailure = input<ClassifiedFailure | null>(null);
  readonly save = output<string>();

  // Required per REQ-CTG-002; max length 100 mirrors the server's rule — each
  // with its own sentence (STD-UX-017; the max-length copy is the shared
  // `validation.maxLength`, no longer the required wording).
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [vfValidators.required, vfValidators.maxLength(100)],
    }),
  });

  protected readonly nameMessages: RuleMessageOverrides = { required: 'categories.error.required' };

  protected readonly operationError = signal<string | null>(null);

  private readonly guide = viewChild(SubmitGuidanceDirective);
  private readonly operationBanner = viewChild('operationBanner', { read: ElementRef });

  protected readonly title = computed(() =>
    this.mode() === 'rename'
      ? this.t.t('categories.dialog.rename.title')
      : this.t.t('categories.dialog.create.title'),
  );

  constructor() {
    // Prime the field when the dialog opens (rename pre-fills the current
    // name): a fresh moment cycle — untouched, pristine, no stale message,
    // no success chrome carried over (STD-UX-014).
    effect(() => {
      if (this.visible()) {
        this.form.controls.name.setValue(this.initialName());
        this.form.markAsUntouched();
        this.form.markAsPristine();
        this.operationError.set(null);
        this.guide()?.resetSubmitted();
      }
    });

    // Classify the page's failure into the two surfaces: a projected field
    // error (STD-UX-019) or the dialog's own operation message — never only
    // a banner for a field fact, never a field line for an operation fact
    // (STD-UX-020).
    effect(() => {
      const failure = this.serverFailure();
      if (!failure) {
        this.operationError.set(null);
        return;
      }

      if (failure.kind === 'field' && failure.fieldErrors) {
        const unmatched = projectServerFieldErrors(this.form, failure.fieldErrors, {
          name: 'categories.error.duplicate',
        });
        if (unmatched.length === 0) {
          this.operationError.set(null);
          return;
        }
      }

      this.operationError.set(
        this.t.t(failure.kind === 'field' ? 'categories.error.saveFailed' : failure.messageKey),
      );
    });

    // The operation message receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.operationError()) {
        return;
      }

      const banner = this.operationBanner()?.nativeElement;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

  /** The footer button lives outside the form element, so it triggers the shared guidance. */
  protected onSave(): void {
    this.guide()?.trigger();
  }

  protected emitSave(): void {
    this.save.emit(this.form.controls.name.value.trim());
  }
}
