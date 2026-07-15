import { Injectable, computed, signal } from '@angular/core';

import { VfTableColumn } from '../../../shared/ui-kit/table/vf-table.component';
import { MessageKey } from '../../../core/i18n/ar';

export interface ProductColumnDef {
  readonly id: string;
  readonly labelKey: MessageKey;
  readonly sortable: boolean;
  readonly numeric: boolean;
  readonly hideable: boolean;
}

export const PRODUCT_COLUMNS: readonly ProductColumnDef[] = [
  { id: 'name', labelKey: 'products.column.name', sortable: true, numeric: false, hideable: false },
  { id: 'category', labelKey: 'products.column.category', sortable: true, numeric: false, hideable: true },
  { id: 'manufacturer', labelKey: 'products.column.manufacturer', sortable: true, numeric: false, hideable: true },
  { id: 'nature', labelKey: 'products.column.nature', sortable: true, numeric: false, hideable: true },
  { id: 'storageUnit', labelKey: 'products.column.storageUnit', sortable: false, numeric: false, hideable: true },
  { id: 'price', labelKey: 'products.column.price', sortable: true, numeric: true, hideable: true },
  { id: 'capabilities', labelKey: 'products.column.capabilities', sortable: false, numeric: false, hideable: true },
  { id: 'status', labelKey: 'products.column.status', sortable: true, numeric: false, hideable: true },
];

/**
 * Column visibility with per-user persistence (catalog ui.md S1:
 * «يتذكر النظام اختيار المستخدم»).
 */
@Injectable()
export class ProductColumnPreferences {
  private readonly hidden = signal<readonly string[]>(readHidden());

  readonly visibleIds = computed(() =>
    PRODUCT_COLUMNS.filter((column) => !this.hidden().includes(column.id)).map((column) => column.id),
  );

  isVisible(columnId: string): boolean {
    return !this.hidden().includes(columnId);
  }

  toggle(columnId: string): void {
    const definition = PRODUCT_COLUMNS.find((column) => column.id === columnId);
    if (!definition?.hideable) {
      return;
    }

    this.hidden.update((hidden) =>
      hidden.includes(columnId) ? hidden.filter((id) => id !== columnId) : [...hidden, columnId],
    );
    persistHidden(this.hidden());
  }

  toTableColumns(labels: (key: MessageKey) => string): readonly VfTableColumn[] {
    return PRODUCT_COLUMNS.filter((column) => this.isVisible(column.id)).map((column) => ({
      id: column.id,
      label: labels(column.labelKey),
      sortable: column.sortable,
      numeric: column.numeric,
    }));
  }
}

const STORAGE_KEY = 'vetflow.catalog.products.hidden-columns.v1';

function readHidden(): readonly string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((id): id is string => typeof id === 'string') : [];
  } catch {
    return [];
  }
}

function persistHidden(hidden: readonly string[]): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(hidden));
  } catch {
    // Storage may be unavailable (private mode); column preferences simply do not persist.
  }
}
