import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { FormatService } from '../../../core/i18n/format.service';
import { TranslationService } from '../../../core/i18n/translation.service';
import { ApiErrorMapper, ClassifiedFailure } from '../../../core/validation/api-error-mapper';
import { ValidationFocusService } from '../../../core/validation/validation-focus.service';
import { VfBannerComponent } from '../../../shared/ui-kit/banner/vf-banner.component';
import { VfButtonComponent } from '../../../shared/ui-kit/button/vf-button.component';
import { VfEmptyStateComponent } from '../../../shared/ui-kit/empty-state/vf-empty-state.component';
import { VfPaginationComponent } from '../../../shared/ui-kit/pagination/vf-pagination.component';
import { VfSearchInputComponent } from '../../../shared/ui-kit/input/vf-search-input.component';
import { CategoriesApiService } from './categories-api.service';
import { CategoryListStore } from './category-list.store';
import { CategoryListItem } from './category-list.models';
import { CategoryCardsComponent } from './components/category-cards.component';
import { CategoryDialogMode, CategoryFormDialogComponent } from './components/category-form-dialog.component';
import { CategoryListSkeletonComponent } from './components/category-list-skeleton.component';
import { CategoryTableComponent } from './components/category-table.component';

/**
 * Screen التصنيفات (categories ui.md, REQ-CTG-001..004): search first, the one
 * table language with the four mandatory states and an adaptive mobile card list,
 * plus create/rename in a dialog and activate/deactivate as direct row actions —
 * every pattern lifted from the product list so it feels native to VetFlow.
 */
@Component({
  selector: 'app-category-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CategoriesApiService, CategoryListStore],
  imports: [
    VfBannerComponent,
    VfSearchInputComponent,
    VfButtonComponent,
    VfEmptyStateComponent,
    VfPaginationComponent,
    CategoryTableComponent,
    CategoryCardsComponent,
    CategoryListSkeletonComponent,
    CategoryFormDialogComponent,
  ],
  template: `
    <div class="page">
      <header class="page-header">
        <h1 class="page-title">{{ t.t('categories.title') }}</h1>
        <vf-button variant="primary" icon="pi-plus" (pressed)="openCreate()">
          {{ t.t('categories.create') }}
        </vf-button>
      </header>

      <div class="toolbar">
        <vf-search-input
          class="toolbar-search"
          [placeholder]="t.t('categories.search.placeholder')"
          [clearLabel]="t.t('categories.search.clear')"
          [autofocus]="true"
          (debouncedValue)="store.setSearch($event)"
        />
      </div>

      @if (toggleFailureMessage(); as message) {
        <vf-banner tone="error" #toggleBanner>{{ message }}</vf-banner>
      }

      <section class="list-area">
        @switch (store.view().kind) {
          @case ('loading') {
            <app-category-list-skeleton />
          }
          @case ('error') {
            <vf-empty-state
              tone="error"
              icon="pi-exclamation-circle"
              [title]="t.t('categories.error.title')"
              [body]="t.t('categories.error.body')"
            >
              <vf-button variant="primary" icon="pi-refresh" (pressed)="store.retry()">
                {{ t.t('categories.error.retry') }}
              </vf-button>
            </vf-empty-state>
          }
          @case ('ready') {
            @switch (store.emptyKind()) {
              @case ('new') {
                <vf-empty-state
                  icon="pi-tags"
                  [title]="t.t('categories.empty.new.title')"
                  [body]="t.t('categories.empty.new.body')"
                >
                  <vf-button variant="primary" icon="pi-plus" (pressed)="openCreate()">
                    {{ t.t('categories.empty.new.action') }}
                  </vf-button>
                </vf-empty-state>
              }
              @case ('search') {
                <vf-empty-state
                  icon="pi-search"
                  [title]="t.t('categories.empty.search.title', { query: store.search() })"
                  [body]="t.t('categories.empty.search.body')"
                />
              }
              @default {
                @if (readyView(); as view) {
                  @if (isMobile()) {
                    <app-category-cards
                      [rows]="view.items"
                      (rename)="openRename($event)"
                      (toggleActive)="toggleActive($event)"
                    />
                  } @else {
                    <app-category-table
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

      <app-category-form-dialog
        [(visible)]="dialogVisible"
        [mode]="dialogMode()"
        [initialName]="dialogInitialName()"
        [saving]="dialogSaving()"
        [serverFailure]="dialogServerFailure()"
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
export class CategoryListPageComponent {
  protected readonly t = inject(TranslationService);
  protected readonly format = inject(FormatService);
  // Public where the template and the component spec both read it, mirroring the
  // product editor page's tested surface.
  readonly store = inject(CategoryListStore);
  private readonly api = inject(CategoriesApiService);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly pageSize = CategoryListStore.PageSize;

  readonly dialogVisible = signal(false);
  readonly dialogMode = signal<CategoryDialogMode>('create');
  readonly dialogInitialName = signal('');
  readonly dialogSaving = signal(false);
  readonly dialogServerFailure = signal<ClassifiedFailure | null>(null);
  /** A failed row toggle — surfaced, never silent (STD-UX-004). */
  readonly toggleFailure = signal<ClassifiedFailure | null>(null);
  private readonly mapper = inject(ApiErrorMapper);
  private readonly focus = inject(ValidationFocusService);
  private readonly toggleBanner = viewChild('toggleBanner', { read: ElementRef });
  private editingId: string | null = null;

  protected readonly toggleFailureMessage = computed(() => {
    const failure = this.toggleFailure();
    return failure ? this.t.t(failure.messageKey, failure.params) : null;
  });

  constructor() {
    // The toggle banner receives focus when it appears (STD-UX-071).
    effect(() => {
      if (!this.toggleFailure()) {
        return;
      }

      const banner = this.toggleBanner()?.nativeElement as HTMLElement | undefined;
      if (banner) {
        this.focus.focusMessage(banner);
      }
    });
  }

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
      return this.t.t('categories.loading');
    }

    if (view.kind === 'error') {
      return this.t.t('categories.error.title');
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
    this.dialogServerFailure.set(null);
    this.dialogVisible.set(true);
  }

  openRename(category: CategoryListItem): void {
    this.editingId = category.id;
    this.dialogMode.set('rename');
    this.dialogInitialName.set(category.name);
    this.dialogServerFailure.set(null);
    this.dialogVisible.set(true);
  }

  onDialogSave(name: string): void {
    const request: Observable<unknown> =
      this.dialogMode() === 'rename' && this.editingId
        ? this.api.rename(this.editingId, name)
        : this.api.create(name);

    this.dialogSaving.set(true);
    this.dialogServerFailure.set(null);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.dialogSaving.set(false);
        this.dialogVisible.set(false);
        this.store.refresh();
      },
      error: (error: unknown) => {
        this.dialogSaving.set(false);
        // Classified by the shared mapper (STD-UX-123); the dialog projects a
        // VTF-VAL-001 `name` failure onto the field (BR-CTG-003 duplicate,
        // STD-UX-019) and renders anything else as its operation message.
        this.dialogServerFailure.set(
          this.mapper.map(error, {
            system: 'categories.error.saveFailed',
            notFound: 'categories.error.saveFailed',
          }),
        );
      },
    });
  }

  toggleActive(category: CategoryListItem): void {
    const request: Observable<void> = category.isActive
      ? this.api.deactivate(category.id)
      : this.api.activate(category.id);

    // Re-read from the server either way — the truth is authoritative, never
    // an optimistic local flip (STD-FE-036). A failure is surfaced through
    // the classified banner, never swallowed (STD-UX-004).
    this.toggleFailure.set(null);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.store.refresh(),
      error: (error: unknown) => {
        this.toggleFailure.set(this.mapper.map(error));
        this.store.refresh();
      },
    });
  }
}
