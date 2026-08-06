import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { Dashboard, DashboardCountSection } from './dashboard.models';
import { DashboardApiService } from './dashboard-api.service';
import { DashboardStore } from './dashboard.store';

describe('DashboardStore', () => {
  function dashboard(overrides: Partial<Dashboard['sections']> = {}): Dashboard {
    const ok = (count: number): DashboardCountSection => ({ status: 'ok', count });
    return {
      clinicDate: '2026-08-03',
      sections: {
        expiredBatches: ok(0),
        outOfStockProducts: ok(0),
        expiringSoonBatches: ok(0),
        draftPurchases: ok(0),
        draftSales: ok(0),
        todaySales: { status: 'ok', count: 0, total: { amount: 0, currency: 'EGP' } },
        recentMovements: { status: 'ok', items: [] },
        ...overrides,
      },
    };
  }

  function createStore(result: Dashboard | 'error'): DashboardStore {
    TestBed.configureTestingModule({
      providers: [
        DashboardStore,
        {
          provide: DashboardApiService,
          useValue: {
            getDashboard: () =>
              result === 'error' ? throwError(() => new Error('boom')) : of(result),
          },
        },
      ],
    });

    const store = TestBed.inject(DashboardStore);

    // `toObservable` bridges the reload signal through an effect, so nothing is fetched until
    // effects run. Without this the store is still on its `loading` seed and every assertion
    // below would be reading the initial value rather than the result.
    TestBed.tick();
    return store;
  }

  it('drops zero attention items entirely (BR-DSH-013)', () => {
    const store = createStore(
      dashboard({ expiredBatches: { status: 'ok', count: 3 }, draftSales: { status: 'ok', count: 0 } }),
    );

    const keys = store.attentionItems().map((item) => item.key);

    expect(keys).toEqual(['expiredBatches']);
  });

  it('orders attention items by fixed severity, never by count (BR-DSH-012)', () => {
    const store = createStore(
      dashboard({
        // The least severe carries by far the largest number — and must still come last.
        draftSales: { status: 'ok', count: 99 },
        expiredBatches: { status: 'ok', count: 1 },
        outOfStockProducts: { status: 'ok', count: 2 },
      }),
    );

    expect(store.attentionItems().map((item) => item.key)).toEqual([
      'expiredBatches',
      'outOfStockProducts',
      'draftSales',
    ]);
  });

  it('reports all-clear when every attention section was read and is zero (BR-DSH-013)', () => {
    expect(createStore(dashboard()).allClear()).toBe(true);
  });

  it('does not report all-clear while any attention item is non-zero (BR-DSH-013)', () => {
    expect(createStore(dashboard({ draftSales: { status: 'ok', count: 1 } })).allClear()).toBe(false);
  });

  it('never reports all-clear when a section failed — a failure is not calm (BR-DSH-014)', () => {
    const store = createStore(dashboard({ expiredBatches: { status: 'failed' } }));

    // Every other section is a genuine zero, so a naive "no items to show" check would call
    // this a quiet morning. It is not: we do not know whether stock has expired.
    expect(store.allClear()).toBe(false);
  });

  it('keeps a failed section visible instead of hiding it like a zero (BR-DSH-014)', () => {
    const store = createStore(dashboard({ expiredBatches: { status: 'failed' } }));

    const items = store.attentionItems();

    expect(items.map((item) => item.key)).toEqual(['expiredBatches']);
    expect(items[0].section.status).toBe('failed');
    expect(items[0].section.count).toBeUndefined();
  });

  it('surfaces a whole-board failure as an error state', () => {
    expect(createStore('error').viewState().kind).toBe('error');
  });

  it('links every attention item to a destination (BR-DSH-002)', () => {
    const store = createStore(
      dashboard({
        expiredBatches: { status: 'ok', count: 1 },
        outOfStockProducts: { status: 'ok', count: 1 },
        expiringSoonBatches: { status: 'ok', count: 1 },
        draftPurchases: { status: 'ok', count: 1 },
        draftSales: { status: 'ok', count: 1 },
      }),
    );

    for (const item of store.attentionItems()) {
      expect(item.routerLink).toMatch(/^\//);
      expect(Object.keys(item.queryParams).length).toBeGreaterThan(0);
    }
  });
});
