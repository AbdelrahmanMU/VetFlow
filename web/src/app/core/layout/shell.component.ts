import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { TranslationService } from '../i18n/translation.service';
import { VfLogoComponent } from '../../shared/ui-kit/logo/vf-logo.component';

/**
 * The application shell (design language §5): the navigation sidebar sits on
 * the right — where Arabic reading starts — calm, without color blocks; the
 * active module is marked by a thin accent. Cross-module top-bar concerns
 * (global search, notifications, account) arrive with their own slices.
 *
 * <b>Below 768 px the same sidebar becomes a drawer</b> — one component, not a
 * second navigation — per the owner's amendment to design-language §5
 * (2026-08-02), which replaced «تنقّل سفلي» with a collapsible sidebar on the
 * mobile tier as well. It slides from the right (reading direction), and closes
 * on the backdrop, on `Esc`, and on choosing a destination. <b>Desktop is
 * untouched:</b> every rule above the breakpoint is exactly what it was.
 *
 * The drawer is `position: fixed`, so opening it neither reflows nor shifts the
 * content behind it.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, VfLogoComponent],
  // On the host rather than the <nav>: Esc must work wherever focus sits while the
  // drawer covers the page, and the Tab trap reads document.activeElement anyway.
  // The handler is inert unless the drawer is actually open on the compact tier.
  host: { '(keydown)': 'onDrawerKeydown($event)' },
  template: `
    <div class="shell" [class.shell--compact]="isCompact()">
      @if (isCompact()) {
        <header class="topbar">
          <button
            #menuButton
            type="button"
            class="menu-button"
            aria-controls="app-nav"
            [attr.aria-expanded]="drawerOpen()"
            [attr.aria-label]="t.t('nav.menu.open')"
            (click)="toggleDrawer()"
          >
            <i class="pi pi-bars" aria-hidden="true"></i>
          </button>
          <vf-logo [height]="26" />
        </header>
      }

      @if (isCompact() && drawerOpen()) {
        <!-- A real button, so dismissing by pointer is not a click handler bolted
             onto a div. The keyboard never lands here — focus is trapped inside the
             drawer — so Esc and the drawer's own close button are the keyboard
             routes out; this carries the same name for the same action. -->
        <button
          type="button"
          class="backdrop"
          [attr.aria-label]="t.t('nav.menu.close')"
          (click)="closeDrawer()"
        ></button>
      }

      <nav
        #drawer
        id="app-nav"
        class="sidebar"
        [class.sidebar--drawer]="isCompact()"
        [class.sidebar--open]="drawerOpen()"
        [attr.aria-label]="t.t('nav.primary')"
        [attr.aria-hidden]="hidden() ? 'true' : null"
        [attr.inert]="hidden() ? '' : null"
      >
        <div class="drawer-head">
          <vf-logo [height]="30" />
          @if (isCompact()) {
            <button
              type="button"
              class="menu-button close-button"
              [attr.aria-label]="t.t('nav.menu.close')"
              (click)="closeDrawer()"
            >
              <i class="pi pi-times" aria-hidden="true"></i>
            </button>
          }
        </div>
        <ul class="nav-list">
          <li class="nav-module">{{ t.t('nav.catalog') }}</li>
          <li>
            <a
              class="nav-link"
              routerLink="/catalog/products"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-box nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.products') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/categories"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-tags nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.categories') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/manufacturers"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-building nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.manufacturers') }}
            </a>
          </li>
          <li class="nav-module">{{ t.t('nav.purchasing') }}</li>
          <li>
            <a
              class="nav-link"
              routerLink="/purchases"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-receipt nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.purchases') }}
            </a>
          </li>
          <!-- The entry point is the list (DEC-SAL-005 — owner-ruled 2026-07-31, AC-SAL-022). -->
          <li class="nav-module">{{ t.t('nav.sales') }}</li>
          <li>
            <a
              class="nav-link"
              routerLink="/sales"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-shopping-cart nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.salesInvoices') }}
            </a>
          </li>
          <li class="nav-module">{{ t.t('nav.inventory') }}</li>
          <li>
            <a
              class="nav-link"
              routerLink="/inventory"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-inbox nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.inventory') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/inventory/expiry"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-clock nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.expiryMonitoring') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/inventory/history"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-history nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.movementHistory') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/inventory/adjustments/new"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-sliders-h nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.adjustment') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/inventory/write-offs/new"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: true }"
            >
              <i class="pi pi-trash nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.writeOff') }}
            </a>
          </li>
        </ul>

        <!-- Signing out lives at the foot of the sidebar, separated from the navigation links
             (identity/ui.md). It is not a destination, so it is a button, not a link — and there
             is no settings screen to put it in, because none exists. -->
        <div class="nav-footer">
          <button type="button" class="nav-link nav-logout" (click)="signOut()">
            <i class="pi pi-sign-out nav-icon" aria-hidden="true"></i>
            {{ t.t('nav.logout') }}
          </button>
        </div>
      </nav>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    /* Grid columns follow the inline direction: in RTL the first column is the
       right edge — where the navigation lives (design language §5). */
    .shell {
      display: grid;
      grid-template-columns: var(--vf-sidebar-width) 1fr;
      grid-template-areas: 'sidebar content';
      min-block-size: 100dvh;
    }

    .sidebar {
      grid-area: sidebar;
      background: var(--vf-surface);
      border-inline-start: 1px solid var(--vf-border);
      padding: var(--vf-space-5) var(--vf-space-4);
    }

    .drawer-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--vf-space-2);
      margin-block-end: var(--vf-space-6);
    }

    /* The wordmark used to be styled text here; it is now <vf-logo>, which owns
       its own presentation. Only the alignment padding stays. */
    .drawer-head vf-logo {
      padding-inline: var(--vf-space-2);
    }

    .nav-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .nav-module {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      padding-inline: var(--vf-space-2);
      margin-block-end: var(--vf-space-1);
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      padding: var(--vf-space-2) var(--vf-space-2);
      border-radius: var(--vf-radius-small);
      color: var(--vf-text-secondary);
      text-decoration: none;
      border-inline-start: 2px solid transparent;
    }

    .nav-link:hover {
      background: var(--vf-bg);
      color: var(--vf-text);
    }

    .nav-link--active {
      color: var(--vf-primary-strong);
      background: var(--vf-primary-soft);
      border-inline-start-color: var(--vf-primary);
      font-weight: 600;
    }

    .nav-icon {
      font-size: 1rem;
    }

    .nav-footer {
      margin-block-start: var(--vf-space-5);
      padding-block-start: var(--vf-space-3);
      border-block-start: 1px solid var(--vf-border);
    }

    /* A button styled as a navigation row: same target size and rhythm as the links
       beside it, without pretending to be a destination. */
    .nav-logout {
      inline-size: 100%;
      font: inherit;
      background: none;
      cursor: pointer;
      border-block: 0;
      border-inline-end: 0;
      text-align: start;
    }

    .content {
      grid-area: content;
      min-inline-size: 0;
      display: flex;
      flex-direction: column;
    }

    /* ---- The compact tier (≤768 px): the same sidebar, as a drawer ----
       Driven by a class, not only a media query, so the component's own
       breakpoint signal and the styling agree on exactly one source of truth. */

    .shell--compact {
      grid-template-columns: 1fr;
      grid-template-areas:
        'topbar'
        'content';
      grid-template-rows: auto 1fr;
    }

    .topbar {
      grid-area: topbar;
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      padding: var(--vf-space-2) var(--vf-space-3);
      background: var(--vf-surface);
      border-block-end: 1px solid var(--vf-border);
    }

    /* ≥44×44 px touch target (design language §14 / §5 amendment). */
    .menu-button {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      inline-size: 2.75rem;
      block-size: 2.75rem;
      padding: 0;
      border: 1px solid transparent;
      border-radius: var(--vf-radius-small);
      background: transparent;
      color: var(--vf-text);
      font-size: 1.25rem;
      cursor: pointer;
    }

    .menu-button:hover {
      background: var(--vf-bg);
    }

    .backdrop {
      position: fixed;
      inset: 0;
      z-index: 40;
      background: var(--vf-overlay);
    }

    /* Fixed, so opening the drawer never reflows or shifts the content behind it.
       It docks on the inline-start edge — the same edge the permanent sidebar
       occupies on desktop, which under dir="rtl" is the right. */
    .sidebar--drawer {
      position: fixed;
      z-index: 50;
      inset-block: 0;
      inset-inline-start: 0;
      inline-size: min(var(--vf-sidebar-width), 82vw);
      overflow-y: auto;
      border-inline-start: 0;
      border-inline-end: 1px solid var(--vf-border);
      box-shadow: var(--vf-shadow-overlay);
      /* Hidden once the slide-out has finished, so the closed drawer is neither
         focusable nor announced. The delay is on the CLOSE direction only —
         opening flips it immediately (see .sidebar--open), because an element
         whose visibility is mid-transition cannot take focus, and the drawer
         must be focusable the moment it opens. */
      visibility: hidden;
      transition:
        transform 200ms ease,
        visibility 0s linear 200ms;
      /* Closed: parked off-screen past the inline-start edge. translateX is
         physical, so the direction is spelled out per writing mode rather than
         assumed — under RTL the inline-start edge is the right one. */
      transform: translateX(-100%);
    }

    :host-context([dir='rtl']) .sidebar--drawer {
      transform: translateX(100%);
    }

    :host-context([dir='rtl']) .sidebar--drawer.sidebar--open {
      transform: translateX(0);
    }

    .sidebar--drawer.sidebar--open {
      transform: translateX(0);
      visibility: visible;
      transition:
        transform 200ms ease,
        visibility 0s;
    }

    .sidebar--drawer .nav-link {
      min-block-size: 2.75rem;
    }
  `,
})
export class ShellComponent {
  protected readonly t = inject(TranslationService);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  private readonly menuButton = viewChild<ElementRef<HTMLButtonElement>>('menuButton');
  private readonly drawer = viewChild<ElementRef<HTMLElement>>('drawer');

  /** The compact tier of design-language §5 — tablet and mobile share one pattern. */
  protected readonly isCompact = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly drawerOpen = signal(false);

  /**
   * The drawer is hidden from assistive technology and taken out of the tab order
   * while closed. `inert` matters as much as `aria-hidden`: a translated-away
   * element is still focusable, and Tab would otherwise walk into an invisible menu.
   */
  protected readonly hidden = computed(() => this.isCompact() && !this.drawerOpen());

  constructor() {
    // Crossing back to desktop must not leave the drawer state armed: the sidebar
    // becomes permanent there, and a stale `true` would re-open it on the way back.
    effect(() => {
      if (!this.isCompact()) {
        this.drawerOpen.set(false);
      }
    });

    // Focus follows the drawer, in both directions (design language §14).
    //
    // Deferred by a task on purpose. `inert` is removed by the same change that
    // opens the drawer, and focus() inside a still-inert subtree is silently
    // ignored — the drawer opened with focus left behind on the page underneath.
    // Browser verification caught this; jsdom cannot, because it does not
    // implement `inert` at all, so the unit test focused happily either way.
    effect(() => {
      if (!this.isCompact() || !this.drawerOpen()) {
        return;
      }

      setTimeout(() => {
        if (this.drawerOpen()) {
          this.firstFocusable()?.focus();
        }
      });
    });

    // Choosing a destination closes the drawer. Keyed off the router rather than a
    // click on the list, so it closes when navigation actually happens — and it
    // stays correct for the keyboard, where Enter on a link is not a list click.
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.closeDrawer());
  }

  /**
   * Signing out is an explicit act and ends the session immediately (BR-IDN-009, AC-IDN-009):
   * the token is discarded and the login screen replaces the app. No confirmation dialog — the
   * cost of signing out by accident is signing in again, and no ruled flow asks for one.
   */
  protected signOut(): void {
    this.auth.signOut();
    void this.router.navigate(['/login']);
  }

  protected toggleDrawer(): void {
    this.drawerOpen.update((open) => !open);
  }

  protected closeDrawer(): void {
    if (!this.drawerOpen()) {
      return;
    }

    this.drawerOpen.set(false);
    this.menuButton()?.nativeElement.focus();
  }


  protected onDrawerKeydown(event: KeyboardEvent): void {
    if (!this.isCompact() || !this.drawerOpen()) {
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      this.closeDrawer();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    // Focus stays inside the open drawer: it covers the page behind it, so tabbing
    // out would land on content the user cannot see.
    const focusable = this.focusableElements();
    const first = focusable.at(0);
    const last = focusable.at(-1);
    if (!first || !last) {
      return;
    }

    const active = document.activeElement;
    if (event.shiftKey && active === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private focusableElements(): HTMLElement[] {
    const root = this.drawer()?.nativeElement;
    return root ? Array.from(root.querySelectorAll<HTMLElement>('a[href], button:not([disabled])')) : [];
  }

  private firstFocusable(): HTMLElement | undefined {
    return this.focusableElements().at(0);
  }
}
