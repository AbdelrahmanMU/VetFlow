import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { DialogModule } from 'primeng/dialog';

/**
 * Modal dialog (design language §9): the confirmation/warning surface — used for
 * the possible-duplicate comparison (catalog ui.md §5). Focus is trapped and
 * returned by the underlying overlay; Escape closes. Wraps the component
 * foundation; features never import PrimeNG directly (ADR-0012, STD-FE-003).
 */
@Component({
  selector: 'vf-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogModule],
  template: `
    <p-dialog
      [(visible)]="visible"
      [header]="header()"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [style]="{ width: '38rem', maxWidth: '94vw' }"
      styleClass="vf-dialog"
    >
      <div class="dialog-body">
        <ng-content />
      </div>
      <ng-template pTemplate="footer">
        <div class="dialog-footer">
          <ng-content select="[dialogFooter]" />
        </div>
      </ng-template>
    </p-dialog>
  `,
  styles: `
    ::ng-deep .vf-dialog {
      font-family: var(--vf-font);
    }

    ::ng-deep .vf-dialog .p-dialog-header {
      padding: var(--vf-space-4) var(--vf-space-5);
      font-weight: 600;
    }

    ::ng-deep .vf-dialog .p-dialog-content {
      padding-inline: var(--vf-space-5);
    }

    .dialog-body {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
    }

    .dialog-footer {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      gap: var(--vf-space-2);
    }
  `,
})
export class VfDialogComponent {
  readonly header = input('');
  readonly visible = model(false);
}
