import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../../shared/ui-kit/button/vf-button.component';
import { VfDialogComponent } from '../../../../shared/ui-kit/dialog/vf-dialog.component';
import { VfTextInputComponent } from '../../../../shared/ui-kit/input/vf-text-input.component';

export type ManufacturerDialogMode = 'create' | 'rename';

/**
 * Create/rename dialog: one Arabic-name field (REQ-CAT-013) behind the shared
 * VfDialog + VfTextInput. A presentation component — the page owns the write. The
 * name field surfaces the server's per-field message (BR-CAT-007 duplicate) or the
 * client's required rule; opening the dialog resets the value, the submitted flag,
 * and any stale error (so a duplicate seen on one manufacturer never lingers onto
 * the next). A deliberate mirror of the category form dialog.
 */
@Component({
  selector: 'app-manufacturer-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, VfDialogComponent, VfTextInputComponent, VfButtonComponent],
  template: `
    <vf-dialog [header]="title()" [(visible)]="visible">
      <vf-text-input
        [label]="t.t('manufacturers.dialog.name')"
        [required]="true"
        [formControl]="nameControl"
        [error]="fieldError()"
      />
      <div dialogFooter>
        <vf-button variant="primary" icon="pi-check" [disabled]="saving()" (pressed)="onSave()">
          {{ saving() ? t.t('manufacturers.dialog.saving') : t.t('manufacturers.dialog.save') }}
        </vf-button>
        <vf-button variant="quiet" [disabled]="saving()" (pressed)="visible.set(false)">
          {{ t.t('manufacturers.dialog.cancel') }}
        </vf-button>
      </div>
    </vf-dialog>
  `,
})
export class ManufacturerFormDialogComponent {
  protected readonly t = inject(TranslationService);

  readonly visible = model(false);
  readonly mode = input<ManufacturerDialogMode>('create');
  readonly initialName = input('');
  readonly saving = input(false);
  readonly serverError = input<string | null>(null);
  readonly save = output<string>();

  protected readonly nameControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(100)],
  });

  protected readonly submitted = signal(false);
  private readonly serverErrorLocal = signal<string | null>(null);

  protected readonly title = computed(() =>
    this.mode() === 'rename'
      ? this.t.t('manufacturers.dialog.rename.title')
      : this.t.t('manufacturers.dialog.create.title'),
  );

  protected readonly fieldError = computed(() => {
    const server = this.serverErrorLocal();
    if (server) {
      return server;
    }

    return this.submitted() && this.nameControl.invalid ? this.t.t('manufacturers.error.required') : null;
  });

  constructor() {
    // Mirror the parent's server error into local display state.
    effect(() => this.serverErrorLocal.set(this.serverError()));

    // Prime the field when the dialog opens (rename pre-fills the current name).
    effect(() => {
      if (this.visible()) {
        this.nameControl.setValue(this.initialName());
        this.nameControl.markAsUntouched();
        this.submitted.set(false);
      }
    });

    // A stale server error clears as soon as the user edits.
    this.nameControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.serverErrorLocal.set(null));
  }

  protected onSave(): void {
    this.submitted.set(true);
    this.nameControl.markAllAsTouched();
    if (this.nameControl.invalid) {
      return;
    }

    this.save.emit(this.nameControl.value.trim());
  }
}
