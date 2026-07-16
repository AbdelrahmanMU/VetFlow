import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { ApiError } from '../../../core/api/problem-details';
import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { VfSearchInputComponent } from '../../../shared/ui-kit/input/vf-search-input.component';
import { ManufacturersApiService } from './manufacturers-api.service';
import { ManufacturerListStore } from './manufacturer-list.store';
import { ManufacturerListItem } from './manufacturer-list.models';
import { ManufacturerCardsComponent } from './components/manufacturer-cards.component';
import { ManufacturerDialogMode, ManufacturerFormDialogComponent } from './components/manufacturer-form-dialog.component';
import { ManufacturerListSkeletonComponent } from './components/manufacturer-list-skeleton.component';
import { ManufacturerTableComponent } from './components/manufacturer-table.component';

/**
 * Screen الشركات المصنعة (Catalog managed data, REQ-CAT-047/048): search first, the
 * one table language with the four mandatory states and an adaptive mobile card list,
 * plus create/rename in a dialog and activate/deactivate as direct row actions —
 * every pattern lifted from the product/category lists so it feels native to VetFlow.
 * A deliberate mirror of the category list page.
 */
@Component({
  selector: 'app-manufacturer-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ManufacturersApiService, ManufacturerListStore],
  imports: [
    VfSearchInputComponent,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfPaginationComponent,
    ManufacturerTableComponent,
    ManufacturerCardsComponent,
    ManufacturerListSkeletonComponent,
    ManufacturerFormDialogComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('manufacturers.title') }}</h1>
        <vf-button variant="primary" icon="pi-plus" (pressed)="openCreate()">
          {{ t.t('manufacturers.create') }}
        </vf-button>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('manufacturers.search.placeholder')"
          [clearLabel]="t.t('manufacturers.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
      </div>

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-manufacturer-list-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('manufacturers.error.title')"
              [body]="t.t('manufacturers.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('manufacturers.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('new') {
                <vf-empty-state
                  icon="pi-building"
                  [title]="t.t('manufacturers.empty.new.title')"
                  [body]="t.t('manufacturers.empty.new.body')"
                >
                  <vf-button variant="primary" icon="pi-plus" (pressed)="openCreate()">
                    {{ t.t('manufacturers.empty.new.action') }}
                  </vf-button>
                </vf-empty-state>
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('manufacturers.empty.search.title', { query: store.search() })"
                  [body]="t.t('manufacturers.empty.search.body')"
                />
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-manufacturer-cards
                      [rows]="view.items"
                      (rename)="openRename($event)"
                      (toggleActive)="toggleActive($event)"
                    />
                  } @else {
                    <app-manufacturer-table
                      [rows]="view.items"
                      [sort]="store.sort()"
                      (sortChange)="store.setSort($event)"
                      (rename)="openRename($event)"
                      (toggleActive)="toggleActive($event)"
                    />
                  }
                  <vf-pagination
                    [page]="store.page()"
                    [pageSize]="pageSize"
                    [totalCount]="view.totalCount"
                    (pageChange)="store.setPage($event)"
                  />
                }
              }
            }
          }
        }
      </section>

      <p class="vf-visually-hidden" aria-live="polite">{{ announcement() }}</p>

      <app-manufacturer-form-dialog
        [(visible)]="dialogVisible"
        [mode]="dialogMode()"
        [initialName]="dialogInitialName()"
        [saving]="dialogSaving()"
        [serverError]="dialogServerError()"
        (save)="onDialogSave($event)"
      />
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      block-size: 100dvh;
      max-inline-size: var(--vf-content-max-width);
      inline-size: 100%;
      margin-inline: auto;
      padding: var(--vf-space-5) var(--vf-space-6);
      gap: var(--vf-space-4);
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-3);
    }

    .page-title {
      margin: 0;
      font-size: var(--vf-text-page-title);
      font-weight: 700;
    }

    .toolbar {
      display: flex;
      align-items: center;
      gap: var(--vf-space-3);
    }

    .toolbar-search {
      flex: 1;
      max-inline-size: 34rem;
    }

    .list-area {
      flex: 1;
      min-block-size: 0;
      display: flex;
      flex-direction: column;
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      overflow: hidden;
    }

    @media (max-width: 768px) {
      .page {
        padding: var(--vf-space-4);
      }

      .list-area {
        background: transparent;
        border: none;
      }
    }
  `,
})
export class ManufacturerListPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  // Public where the template and the component spec both read it, mirroring the
  // category list page's tested surface.
  readonly store = inject(ManufacturerListStore);
  private readonly api = inject(ManufacturersApiService);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly pageSize = ManufacturerListStore.PageSize;

  readonly dialogVisible = signal(false);
  readonly dialogMode = signal<ManufacturerDialogMode>('create');
  readonly dialogInitialName = signal('');
  readonly dialogSaving = signal(false);
  readonly dialogServerError = signal<string | null>(null);
  private editingId: string | null = null;

  protected readonly isMobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly readyView = computed(() => {
    const view = this.store.view();
    return view.kind === 'ready' ? view : null;
  });

  protected readonly announcement = computed(() => {
    const view = this.store.view();
    if (view.kind === 'loading') {
      return this.t.t('manufacturers.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('manufacturers.error.title');
    }

    return view.totalCount === 0
      ? this.t.t('pagination.zero')
      : this.t.t('pagination.range', {
          from: this.format.integer((this.store.page() - 1) * this.pageSize + 1),
          to: this.format.integer(Math.min(this.store.page() * this.pageSize, view.totalCount)),
          total: this.format.integer(view.totalCount),
        });
  });

  openCreate(): void {
    this.editingId = null;
    this.dialogMode.set('create');
    this.dialogInitialName.set('');
    this.dialogServerError.set(null);
    this.dialogVisible.set(true);
  }

  openRename(manufacturer: ManufacturerListItem): void {
    this.editingId = manufacturer.id;
    this.dialogMode.set('rename');
    this.dialogInitialName.set(manufacturer.name);
    this.dialogServerError.set(null);
    this.dialogVisible.set(true);
  }

  onDialogSave(name: string): void {
    const request: Observable<unknown> =
      this.dialogMode() === 'rename' && this.editingId
        ? this.api.rename(this.editingId, name)
        : this.api.create(name);

    this.dialogSaving.set(true);
    this.dialogServerError.set(null);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.dialogSaving.set(false);
        this.dialogVisible.set(false);
        this.store.refresh();
      },
      error: (error: unknown) => {
        this.dialogSaving.set(false);
        this.dialogServerError.set(this.fieldErrorFrom(error));
      },
    });
  }

  toggleActive(manufacturer: ManufacturerListItem): void {
    const request: Observable<void> = manufacturer.isActive
      ? this.api.deactivate(manufacturer.id)
      : this.api.activate(manufacturer.id);

    // Re-read from the server either way — the truth is authoritative, never
    // an optimistic local flip (STD-FE-036).
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.store.refresh(),
      error: () => this.store.refresh(),
    });
  }

  /**
   * The name field's message. A per-field validation failure on an already
   * client-validated name (required + length are enforced before submit) means the
   * uniqueness rule fired (BR-CAT-007), so we show a local Arabic message rather
   * than the server's Accept-Language-negotiated text — the UI is Arabic-only
   * (ADR-0007). Decided on the structured error (errorCode + the `name` field),
   * never on message text (STD-FE-037); any other failure is the generic fallback.
   */
  private fieldErrorFrom(error: unknown): string {
    if (
      error instanceof ApiError &&
      error.problem?.errorCode === 'VTF-VAL-001' &&
      (error.problem.errors?.['name']?.length ?? 0) > 0
    ) {
      return this.t.t('manufacturers.error.duplicate');
    }

    return this.t.t('manufacturers.error.saveFailed');
  }
}
