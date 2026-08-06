import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, startWith, switchMap } from 'rxjs/operators';

import { ATTENTION_ORDER, AttentionItem, Dashboard } from './dashboard.models';
import { DashboardApiService } from './dashboard-api.service';

export type DashboardViewState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly dashboard: Dashboard };

/**
 * Operational dashboard state (REQ-DSH-010): signals for state, RxJS only at the HTTP
 * boundary (STD-FE-012/013).
 *
 * **No polling, no timer, no auto-refresh** (BR-DSH-015, DEC-DSH-010). BR-INV-032 forbids
 * scheduled refresh and background jobs in read screens, and a board read at the start of a
 * shift is worse when a stale number *looks* live. Reloading is an explicit user act.
 */
@Injectable()
export class DashboardStore {
  private readonly api = inject(DashboardApiService);

  private readonly reloadCounter = signal(0);

  private readonly state = toSignal(
    toObservable(this.reloadCounter).pipe(
      switchMap(() =>
        this.api.getDashboard().pipe(
          map((dashboard) => ({ kind: 'ready', dashboard }) as DashboardViewState),
          startWith({ kind: 'loading' } as DashboardViewState),
          catchError(() => of({ kind: 'error' } as DashboardViewState)),
        ),
      ),
    ),
    { initialValue: { kind: 'loading' } as DashboardViewState },
  );

  readonly viewState = computed(() => this.state());

  readonly dashboard = computed(() => {
    const current = this.state();
    return current.kind === 'ready' ? current.dashboard : null;
  });

  /**
   * The attention items worth showing, in the fixed severity order (BR-DSH-012).
   *
   * **A zero item is dropped entirely** (BR-DSH-013) — not greyed out, not rendered as an
   * empty tile. **A failed section is kept**, because "could not determine" is information
   * the owner must see; it renders as a failure, never as a zero (BR-DSH-014).
   */
  readonly attentionItems = computed<readonly AttentionItem[]>(() => {
    const dashboard = this.dashboard();
    if (!dashboard) {
      return [];
    }

    return ATTENTION_ORDER.map((item) => ({
      ...item,
      section: dashboard.sections[item.key],
    })).filter((item) => item.section.status === 'failed' || (item.section.count ?? 0) > 0);
  });

  /**
   * True when the clinic is genuinely quiet: every attention section was read successfully
   * and every one of them is zero.
   *
   * **A failure is not calm.** If any section could not be read we cannot claim all-clear,
   * so the guard is `status === 'ok'` on all five and not merely an empty item list — which
   * is the difference BR-DSH-013 and BR-DSH-014 exist to protect.
   */
  readonly allClear = computed(() => {
    const dashboard = this.dashboard();
    if (!dashboard) {
      return false;
    }

    return ATTENTION_ORDER.every((item) => {
      const section = dashboard.sections[item.key];
      return section.status === 'ok' && (section.count ?? 0) === 0;
    });
  });

  reload(): void {
    this.reloadCounter.update((value) => value + 1);
  }
}
