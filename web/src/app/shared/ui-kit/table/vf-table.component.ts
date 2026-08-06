import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  contentChild,
  input,
  output,
} from '@angular/core';
import { TableModule } from 'primeng/table';

export interface VfTableColumn {
  readonly id: string;
  readonly label: string;
  readonly sortable: boolean;
  readonly numeric?: boolean;
}

export interface VfSortState {
  readonly field: string;
  readonly direction: 'asc' | 'desc';
}

/**
 * The one table language (design language §6): sticky header, comfortable
 * density, light horizontal separators — no full grid, no zebra striping.
 * Server-side sorting via header buttons; column widths are draggable and
 * remembered. Arrow keys walk the rows (§6 keyboard set).
 */
@Component({
  selector: 'vf-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, TableModule],
  template: `
    <p-table
      [value]="mutableRows()"
      [columns]="mutableColumns()"
      [resizableColumns]="true"
      columnResizeMode="expand"
      [scrollable]="true"
      scrollHeight="flex"
      stateStorage="local"
      [stateKey]="stateKey()"
      styleClass="vf-table"
      [attr.aria-label]="tableLabel()"
      (keydown.arrowdown)="moveRowFocus($event, 1)"
      (keydown.arrowup)="moveRowFocus($event, -1)"
    >
      <ng-template #header let-cols>
        <tr>
          @for (col of cols; track col.id) {
            <th
              pResizableColumn
              [class.vf-th--numeric]="col.numeric"
              [attr.aria-sort]="ariaSort(col)"
            >
              @if (col.sortable) {
                <button type="button" class="vf-sort" (click)="toggleSort(col)">
                  <span>{{ col.label }}</span>
                  @if (sort(); as sortState) {
                    @if (sortState.field === col.id) {
                      <i
                        class="pi vf-sort-icon"
                        [class.pi-sort-up-fill]="sortState.direction === 'asc'"
                        [class.pi-sort-down-fill]="sortState.direction === 'desc'"
                        aria-hidden="true"
                      ></i>
                    }
                  }
                </button>
              } @else {
                <span class="vf-th-label">{{ col.label }}</span>
              }
            </th>
          }
        </tr>
      </ng-template>
      <ng-template #body let-row let-cols="columns">
        <ng-container
          [ngTemplateOutlet]="rowTemplate()"
          [ngTemplateOutletContext]="{ $implicit: row, columns: cols }"
        />
      </ng-template>
    </p-table>
  `,
  styles: `
    :host {
      display: block;
      min-block-size: 0;
    }

    ::ng-deep .vf-table {
      font-family: var(--vf-font);
      font-size: var(--vf-text-body);

      /*
        A touch tighter than the page's 1.8 (§10 gives Arabic prose that room), so a
        row reads as one line of data rather than a paragraph: with the block padding
        below this lands a row at ~48 px — comfortable for long reading without the
        dissipated feel §6 warns against («لا ضيق خانق ولا تباعد مبدَّد»).
      */
      line-height: 1.6;
    }

    ::ng-deep .vf-table .p-datatable-table {
      border-collapse: collapse;
    }

    ::ng-deep .vf-table .p-datatable-thead > tr > th {
      background: var(--vf-bg);
      color: var(--vf-text-secondary);
      font-size: var(--vf-text-secondary-size);
      font-weight: 600;
      text-align: start;
      border-block-end: 1px solid var(--vf-border);
      padding: var(--vf-space-3) var(--vf-space-4);
      white-space: nowrap;
    }

    ::ng-deep .vf-table .p-datatable-tbody > tr {
      background: var(--vf-surface);
      color: var(--vf-text);
      transition: background-color 100ms ease;
    }

    ::ng-deep .vf-table .p-datatable-tbody > tr:hover {
      background: var(--vf-bg);
    }

    ::ng-deep .vf-table .p-datatable-tbody > tr:focus-visible {
      outline: none;
      box-shadow: inset var(--vf-focus-ring);
    }

    ::ng-deep .vf-table .p-datatable-tbody > tr > td {
      border: none;
      border-block-end: 1px solid var(--vf-border);
      padding: var(--vf-space-3) var(--vf-space-4);
      vertical-align: middle;
    }

    /*
      The one numeric standard (§6 as amended by the owner 2026-08-06), applied from
      the column declaration so no screen has to remember it — the same three
      properties shared/styles/_numeric.scss gives the raw-table deviations.

      The inline START edge, not the end: the page is RTL but a number is still an
      LTR run, so the end edge pins its *most significant* digit and the units place
      scatters. The start edge pins the trailing digit, so decimals line up down the
      column and the header — which sits on that same edge — lands directly above
      its value.
    */
    ::ng-deep .vf-table .vf-th--numeric,
    ::ng-deep .vf-table .vf-td--numeric {
      text-align: start;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;

      /* Content width, not an equal share: an over-wide column strands the value
         far from its header. 1% is shrink-to-fit under this table's auto layout. */
      inline-size: 1%;
    }

    .vf-sort {
      display: inline-flex;
      align-items: center;
      gap: var(--vf-space-1);
      border: none;
      background: transparent;
      font: inherit;
      color: inherit;
      cursor: pointer;
      padding: 0;
    }

    .vf-sort:hover {
      color: var(--vf-text);
    }

    .vf-sort-icon {
      font-size: 0.625rem;
      color: var(--vf-primary);
    }
  `,
})
export class VfTableComponent<TRow> {
  readonly rows = input.required<readonly TRow[]>();
  readonly columns = input.required<readonly VfTableColumn[]>();
  readonly sort = input<VfSortState | null>(null);
  readonly stateKey = input.required<string>();
  readonly tableLabel = input('');
  readonly sortChange = output<VfSortState>();

  protected readonly rowTemplate = contentChild.required<TemplateRef<unknown>>('row');

  // The underlying table expects mutable arrays; inputs stay readonly.
  protected readonly mutableRows = computed(() => [...this.rows()]);
  protected readonly mutableColumns = computed(() => [...this.columns()]);

  protected toggleSort(column: VfTableColumn): void {
    const current = this.sort();
    const direction = current?.field === column.id && current.direction === 'asc' ? 'desc' : 'asc';
    this.sortChange.emit({ field: column.id, direction });
  }

  protected ariaSort(column: VfTableColumn): string | null {
    const current = this.sort();
    if (current?.field !== column.id) {
      return null;
    }

    return current.direction === 'asc' ? 'ascending' : 'descending';
  }

  protected moveRowFocus(event: Event, step: number): void {
    const target = event.target as HTMLElement;
    const row = target.closest('tr[tabindex]');
    if (!row) {
      return;
    }

    event.preventDefault();
    const allRows = [...(row.closest('tbody')?.querySelectorAll<HTMLElement>('tr[tabindex]') ?? [])];
    const nextIndex = allRows.indexOf(row as HTMLElement) + step;
    allRows.at(Math.min(Math.max(nextIndex, 0), allRows.length - 1))?.focus();
  }
}
