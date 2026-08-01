import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { Router } from '@angular/router';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../shared/ui-kit/badge/vf-badge.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { ProductDetailsApiService } from './product-details-api.service';
import { ProductDetailsStore } from './product-details.store';

/**
 * Screen S2 — تفاصيل المنتج (catalog ui.md §4): a read-only page answering "what
 * is this product, and can it trade now?" — the four data-view states
 * (STD-FE-030) including a distinct not-found, RTL throughout.
 */
@Component({
  selector: 'app-product-details-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProductDetailsApiService, ProductDetailsStore],
  imports: [VfBadgeComponent, VfButtonComponent, VfEmptyStateComponent],
  template: `
    <div class="page">
      @switch (store.view().kind) {
        @case ('loading') {
          <div class="state" role="status">{{ t.t('productDetails.loading') }}</div>
        }
        @case ('error') {
          <vf-empty-state
            tone="error"
            icon="pi-exclamation-circle"
            [title]="t.t('productDetails.error.title')"
            [body]="t.t('productDetails.error.body')"
          >
            <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
              {{ t.t('productDetails.error.retry') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('notFound') {
          <vf-empty-state
            icon="pi-inbox"
            [title]="t.t('productDetails.notFound.title')"
            [body]="t.t('productDetails.notFound.body')"
          >
            <vf-button variant="secondary" icon="pi-arrow-right" (pressed)="goToList()">
              {{ t.t('productDetails.back') }}
            </vf-button>
          </vf-empty-state>
        }
        @case ('ready') {
          @if (readyProduct(); as product) {
            <header class="header">
              <div class="titles">
                <h1 class="name">{{ product.arabicName }}</h1>
                @if (subtitle(product); as sub) {
                  <p class="subtitle">{{ sub }}</p>
                }
              </div>
              <div class="badges">
                @if (product.status === 'active') {
                  <vf-badge tone="success" icon="pi-check-circle">{{ t.t('products.status.active') }}</vf-badge>
                } @else {
                  <vf-badge tone="neutral" icon="pi-ban">{{ t.t('products.status.disabled') }}</vf-badge>
                }
                @if (!product.hasSellingPrice) {
                  <vf-badge tone="warning" icon="pi-tag">{{ t.t('products.status.noPrice') }}</vf-badge>
                }
              </div>
              <div class="header-actions">
                <vf-button variant="secondary" icon="pi-pencil" (pressed)="goToEdit()">
                  {{ t.t('productDetails.edit') }}
                </vf-button>
                <vf-button variant="quiet" icon="pi-arrow-right" (pressed)="goToList()">
                  {{ t.t('productDetails.back') }}
                </vf-button>
              </div>
            </header>

            <section class="card">
              <h2 class="card-title">{{ t.t('productDetails.section.identity') }}</h2>
              <dl class="facts">
                <div><dt>{{ t.t('productDetails.category') }}</dt><dd>{{ product.categoryName }}</dd></div>
                <div><dt>{{ t.t('productDetails.manufacturer') }}</dt><dd>{{ product.manufacturerName }}</dd></div>
                <div><dt>{{ t.t('productDetails.nature') }}</dt><dd>{{ product.natureName }}</dd></div>
                <div><dt>{{ t.t('productDetails.internalCode') }}</dt><dd class="vf-num">{{ product.internalCode }}</dd></div>
              </dl>
            </section>

            <section class="card">
              <h2 class="card-title">{{ t.t('productDetails.section.capabilities') }}</h2>
              <div class="chips">
                @if (product.isSplittable) {
                  <vf-badge tone="neutral" icon="pi-clone">{{ t.t('products.capability.splittable') }}</vf-badge>
                }
                @if (product.isRefrigerated) {
                  <vf-badge tone="neutral" icon="pi-snowflake">{{ t.t('products.capability.refrigerated') }}</vf-badge>
                }
                @if (product.hasExpiration) {
                  <vf-badge tone="neutral" icon="pi-calendar">{{ t.t('products.capability.hasExpiration') }}</vf-badge>
                }
                @if (product.hasOpenExpiration && product.openExpirationPeriodDays !== null) {
                  <vf-badge tone="neutral" icon="pi-hourglass">
                    {{ t.t('productDetails.openExpirationDays', { days: format.integer(product.openExpirationPeriodDays) }) }}
                  </vf-badge>
                }
              </div>
            </section>

            <section class="card">
              <h2 class="card-title">{{ t.t('productDetails.section.units') }}</h2>
              <div class="table-scroll">
                <table class="units">
                  <thead>
                    <tr>
                      <th>{{ t.t('productDetails.units.unit') }}</th>
                      <th>{{ t.t('productDetails.units.conversion') }}</th>
                      <th>{{ t.t('productDetails.units.role') }}</th>
                      <th>{{ t.t('productDetails.units.barcode') }}</th>
                      <th>{{ t.t('productDetails.units.price') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (unit of product.units; track unit.unitId) {
                      <tr>
                        <td>
                          <span class="unit-name">{{ unit.unitName }}</span>
                          <span class="unit-flags">
                            @if (unit.isStorageUnit) {
                              <vf-badge tone="success" icon="pi-box">{{ t.t('productDetails.units.storage') }}</vf-badge>
                            }
                            @if (unit.isDefaultSaleUnit) {
                              <vf-badge tone="neutral" icon="pi-shopping-cart">{{ t.t('productDetails.units.defaultSale') }}</vf-badge>
                            }
                            @if (unit.isDefaultPurchaseUnit) {
                              <vf-badge tone="neutral" icon="pi-truck">{{ t.t('productDetails.units.defaultPurchase') }}</vf-badge>
                            }
                          </span>
                        </td>
                        <td class="vf-num">{{ unit.quantityInNextUnit === null ? '—' : format.integer(unit.quantityInNextUnit) }}</td>
                        <td>
                          @if (unit.isPurchaseUnit) { <span class="role">{{ t.t('productDetails.units.purchase') }}</span> }
                          @if (unit.isSaleUnit) { <span class="role">{{ t.t('productDetails.units.sale') }}</span> }
                        </td>
                        <td class="vf-num">{{ unit.barcode ?? '—' }}</td>
                        <td class="vf-num">
                          {{ unit.sellingPrice ? format.money(unit.sellingPrice.amount, unit.sellingPrice.currency) : '—' }}
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              @if (!product.hasSellingPrice) {
                <p class="notice">{{ t.t('productDetails.noPrice') }}</p>
              }
            </section>

            <section class="card">
              <h2 class="card-title">{{ t.t('productDetails.section.notes') }}</h2>
              <p class="notes">{{ product.internalNotes ?? t.t('productDetails.noNotes') }}</p>
            </section>
          }
        }
      }

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>
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

    .header {
      display: flex;
      align-items: flex-start;
      gap: var(--vf-space-3);
      flex-wrap: wrap;
    }

    .titles {
      flex: 1;
      min-inline-size: 12rem;
    }

    .name {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .subtitle {
      margin: var(--vf-space-1) 0 0;
      color: var(--vf-text-secondary);
    }

    .badges {
      display: flex;
      gap: var(--vf-space-2);
      align-items: center;
      flex-wrap: wrap;
    }

    .header-actions {
      display: flex;
      gap: var(--vf-space-2);
      align-items: center;
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

    .facts {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      gap: var(--vf-space-3);
      margin: 0;
    }

    .facts dt {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      margin-block-end: var(--vf-space-1);
    }

    .facts dd {
      margin: 0;
      color: var(--vf-text);
    }

    .chips {
      display: flex;
      flex-wrap: wrap;
      gap: var(--vf-space-2);
    }

    .table-scroll {
      overflow-x: auto;
    }

    .units {
      inline-size: 100%;
      border-collapse: collapse;
      font-size: var(--vf-text-body);
    }

    .units th,
    .units td {
      text-align: start;
      padding: var(--vf-space-2) var(--vf-space-3);
      border-block-end: 1px solid var(--vf-border);
      white-space: nowrap;
    }

    .units th {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      font-weight: 600;
    }

    .unit-name {
      font-weight: 500;
    }

    .unit-flags {
      display: inline-flex;
      gap: var(--vf-space-1);
      margin-inline-start: var(--vf-space-2);
    }

    .role {
      display: inline-block;
      margin-inline-end: var(--vf-space-2);
      color: var(--vf-text-secondary);
    }

    .notice {
      margin: var(--vf-space-3) 0 0;
      color: var(--vf-warning);
      font-weight: 500;
    }

    .notes {
      margin: 0;
      color: var(--vf-text-secondary);
      white-space: pre-wrap;
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }
    }
  `,
})
export class ProductDetailsPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  protected readonly store = inject(ProductDetailsStore);
  private readonly router = inject(Router);

  /** Route parameter bound via withComponentInputBinding(). */
  readonly id = input.required<string>();

  constructor() {
    effect(() => this.store.setId(this.id()));
  }

  protected readyProduct() {
    const view = this.store.view();
    return view.kind === 'ready' ? view.product : null;
  }

  // Screen-level load outcomes for the polite live region (STD-UX-092):
  // loading, error, not-found, and the loaded product by name.
  protected readonly announcement = computed(() => {
    const view = this.store.view();
    switch (view.kind) {
      case 'loading':
        return this.t.t('productDetails.loading');
      case 'error':
        return this.t.t('productDetails.error.title');
      case 'notFound':
        return this.t.t('productDetails.notFound.title');
      default:
        return view.product.arabicName;
    }
  });

  protected subtitle(product: { englishName: string | null; size: string | null; concentration: string | null }): string | null {
    const parts = [product.englishName, product.size, product.concentration].filter((part): part is string => !!part);
    return parts.length > 0 ? parts.join(' · ') : null;
  }

  protected goToList(): void {
    void this.router.navigate(['/catalog/products']);
  }

  protected goToEdit(): void {
    void this.router.navigate(['/catalog/products', this.id(), 'edit']);
  }
}
