import { MessageKey } from '../../core/i18n/ar';

/**
 * The operational dashboard contract (REQ-DSH-010) — one response, one status per section
 * (DEC-DSH-002).
 */

/** Whether a section could be read. A failed section carries no data — never a zero. */
export type DashboardSectionStatus = 'ok' | 'failed';

export interface DashboardCountSection {
  readonly status: DashboardSectionStatus;
  /** Absent when the section failed (BR-DSH-014). */
  readonly count?: number;
}

export interface DashboardMoney {
  readonly amount: number;
  readonly currency: string;
}

export interface DashboardTodaySalesSection {
  readonly status: DashboardSectionStatus;
  readonly count?: number;
  readonly total?: DashboardMoney;
}

export interface DashboardMovement {
  readonly movementId: string;
  readonly occurredAt: string;
  /** Inventory's own closed vocabulary (BR-INV-065), passed through untouched. */
  readonly type: string;
  readonly productName: string;
  readonly quantity: number;
  readonly stockUnitName: string;
}

export interface DashboardRecentMovementsSection {
  readonly status: DashboardSectionStatus;
  readonly items?: readonly DashboardMovement[];
}

export interface DashboardSections {
  readonly expiredBatches: DashboardCountSection;
  readonly outOfStockProducts: DashboardCountSection;
  readonly expiringSoonBatches: DashboardCountSection;
  readonly draftPurchases: DashboardCountSection;
  readonly draftSales: DashboardCountSection;
  readonly todaySales: DashboardTodaySalesSection;
  readonly recentMovements: DashboardRecentMovementsSection;
}

export interface Dashboard {
  /**
   * The clinic local date the server used. **Displayed, never recomputed** — deriving a date
   * from the device is a prohibited source (BR-DSH-003, clinic-date.md).
   */
  readonly clinicDate: string;
  readonly sections: DashboardSections;
}

/**
 * One attention item, ready to render.
 *
 * The five are ordered by **severity, fixed** (BR-DSH-012, DEC-DSH-007) — never by count. A
 * tile that moves when its number grows has to be re-read every morning; a tile that stays
 * put is learned once. The order is derived from design-language §4 («الخطر يعلو كلّ شيء
 * حين يظهر»), not from a preference.
 */
export interface AttentionItem {
  readonly key: AttentionKey;
  /** Typed, so an unapproved wording cannot be referenced by mistake (OQ-DSH-3). */
  readonly labelKey: MessageKey;
  readonly icon: string;
  readonly tone: 'warning' | 'danger';
  readonly routerLink: string;
  readonly queryParams: Record<string, string>;
  readonly section: DashboardCountSection;
}

export type AttentionKey =
  | 'expiredBatches'
  | 'outOfStockProducts'
  | 'expiringSoonBatches'
  | 'draftPurchases'
  | 'draftSales';

/**
 * The fixed severity order and each item's destination.
 *
 * **Every filter below already exists inside an approved whitelist** — «نفاد المخزون» in
 * BR-INV-014, «منتهية»/«قرب الانتهاء» in BR-INV-035, status in BR-PUR-004 and BR-SAL-019.
 * No rule is amended and nothing is added to any «حصرًا» list; that is precisely what makes
 * this navigation contract buildable (DEC-DSH-006).
 */
export const ATTENTION_ORDER: readonly Omit<AttentionItem, 'section'>[] = [
  {
    // A safety decision, not a display filter (DEC-INV-021) — so it leads, and it is the one
    // item allowed to outrank the primary action (§4).
    key: 'expiredBatches',
    labelKey: 'dashboard.expiredBatches',
    icon: 'pi-ban',
    tone: 'danger',
    routerLink: '/inventory/expiry',
    queryParams: { expired: 'true' },
  },
  {
    // A sale lost today.
    key: 'outOfStockProducts',
    labelKey: 'dashboard.outOfStock',
    icon: 'pi-inbox',
    tone: 'danger',
    routerLink: '/inventory',
    queryParams: { outOfStock: 'true' },
  },
  {
    // A loss still avoidable.
    key: 'expiringSoonBatches',
    labelKey: 'dashboard.expiringSoon',
    icon: 'pi-clock',
    tone: 'warning',
    routerLink: '/inventory/expiry',
    queryParams: { expiringSoon: 'true' },
  },
  {
    // Goods that may already be here but are not in the system: until this clears, every
    // other number on the board is incomplete (BR-PUR-009/010).
    key: 'draftPurchases',
    labelKey: 'dashboard.draftPurchases',
    icon: 'pi-inbox',
    tone: 'warning',
    routerLink: '/purchases',
    queryParams: { status: 'draft' },
  },
  {
    // Unfinished work, not danger.
    key: 'draftSales',
    labelKey: 'dashboard.draftSales',
    icon: 'pi-file-edit',
    tone: 'warning',
    routerLink: '/sales',
    queryParams: { status: 'draft' },
  },
];
