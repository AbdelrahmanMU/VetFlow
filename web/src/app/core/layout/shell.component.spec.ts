import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { ShellComponent } from './shell.component';

/**
 * The application shell's responsive navigation (design language §5, as amended
 * by the owner on 2026-08-02: the collapsible sidebar is the pattern on the
 * tablet *and* mobile tiers).
 *
 * The contract pinned here is the one the owner enumerated: desktop unchanged ·
 * a hamburger below the breakpoint · a drawer that closes on the backdrop, on
 * Esc, and on choosing a destination · reachable and escapable by keyboard.
 */
describe('ShellComponent — responsive navigation', () => {
  let fixture: ComponentFixture<ShellComponent>;
  let matches: BehaviorSubject<BreakpointState>;

  const state = (isCompact: boolean): BreakpointState => ({ matches: isCompact, breakpoints: {} });

  /** Sets the tier before the component is created, as a real page load would. */
  async function renderAt(compact: boolean): Promise<void> {
    matches = new BehaviorSubject<BreakpointState>(state(compact));

    TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        // The sidebar's sign-out reaches AuthService, which reaches ApiClient for sign-in only;
        // the shell never calls it, but the injector still has to be able to build it.
        provideHttpClient(),
        { provide: BreakpointObserver, useValue: { observe: () => matches.asObservable() } },
      ],
    });

    fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  const menuButton = (): HTMLButtonElement | null =>
    fixture.nativeElement.querySelector('.topbar .menu-button');
  const drawer = (): HTMLElement => fixture.nativeElement.querySelector('#app-nav');
  const backdrop = (): HTMLButtonElement | null => fixture.nativeElement.querySelector('.backdrop');

  async function settle(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    // Focus is deferred by a task so it lands after `inert` is off the drawer;
    // yield a macrotask so the assertions see the settled state.
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  }

  describe('desktop — unchanged', () => {
    it('shows the permanent sidebar with no hamburger and no drawer state', async () => {
      await renderAt(false);

      expect(menuButton()).toBeNull();
      expect(backdrop()).toBeNull();
      expect(drawer().classList.contains('sidebar--drawer')).toBe(false);
      // Never hidden from assistive technology on desktop.
      expect(drawer().getAttribute('aria-hidden')).toBeNull();
      expect(drawer().hasAttribute('inert')).toBe(false);
    });
  });

  describe('compact tier', () => {
    it('collapses the sidebar behind a hamburger, closed and inert to start', async () => {
      await renderAt(true);

      expect(menuButton()).not.toBeNull();
      expect(menuButton()!.getAttribute('aria-expanded')).toBe('false');
      expect(menuButton()!.getAttribute('aria-controls')).toBe('app-nav');
      expect(drawer().classList.contains('sidebar--drawer')).toBe(true);
      expect(drawer().classList.contains('sidebar--open')).toBe(false);

      // Closed means out of the tab order too — a translated-away element is
      // still focusable, and Tab would otherwise walk into an invisible menu.
      expect(drawer().getAttribute('aria-hidden')).toBe('true');
      expect(drawer().hasAttribute('inert')).toBe(true);
    });

    it('opens on the hamburger and moves focus into the drawer', async () => {
      await renderAt(true);

      menuButton()!.click();
      await settle();

      expect(drawer().classList.contains('sidebar--open')).toBe(true);
      expect(menuButton()!.getAttribute('aria-expanded')).toBe('true');
      expect(drawer().hasAttribute('inert')).toBe(false);
      expect(drawer().contains(document.activeElement)).toBe(true);
    });

    it('closes on the backdrop and returns focus to the hamburger', async () => {
      await renderAt(true);
      menuButton()!.click();
      await settle();

      expect(backdrop()).not.toBeNull();
      backdrop()!.click();
      await settle();

      expect(drawer().classList.contains('sidebar--open')).toBe(false);
      expect(backdrop()).toBeNull();
      expect(document.activeElement).toBe(menuButton());
    });

    it('closes on Escape', async () => {
      await renderAt(true);
      menuButton()!.click();
      await settle();

      fixture.nativeElement.dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
      );
      await settle();

      expect(drawer().classList.contains('sidebar--open')).toBe(false);
      expect(document.activeElement).toBe(menuButton());
    });

    it('closes when a destination is chosen', async () => {
      await renderAt(true);
      menuButton()!.click();
      await settle();
      expect(drawer().classList.contains('sidebar--open')).toBe(true);

      await TestBed.inject(Router).navigate(['/catalog/products']);
      await settle();

      expect(drawer().classList.contains('sidebar--open')).toBe(false);
    });

    it('traps Tab inside the open drawer', async () => {
      await renderAt(true);
      menuButton()!.click();
      await settle();

      const focusable = Array.from(
        drawer().querySelectorAll<HTMLElement>('a[href], button:not([disabled])'),
      );
      expect(focusable.length).toBeGreaterThan(1);

      // Forward off the last element wraps to the first.
      focusable.at(-1)!.focus();
      fixture.nativeElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
      await settle();
      expect(document.activeElement).toBe(focusable.at(0));

      // Backward off the first wraps to the last.
      fixture.nativeElement.dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true }),
      );
      await settle();
      expect(document.activeElement).toBe(focusable.at(-1));
    });
  });

  /**
   * Moved here from `app.spec.ts` when the shell stopped being the application root: it is the
   * shell that carries the brand, and this exact assertion caught the accessible name going
   * missing when the text brand became a mark.
   */
  it('brands the sidebar with the shared logo, keeping the product name announceable', async () => {
    await renderAt(false);
    const logo = (fixture.nativeElement as HTMLElement).querySelector('vf-logo img');

    expect(logo).toBeTruthy();
    expect(logo?.getAttribute('alt')).toBe('VetFlow');
    // Referenced, never inlined, so the artwork stays out of the JS bundle (TD-107).
    expect(logo?.getAttribute('src')).toContain('assets/branding/');
  });

  it('offers signing out at the foot of the navigation, and it ends the session (TS-IDN-011, AC-IDN-009)', async () => {
    await renderAt(false);

    const logout = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.nav-logout');
    expect(logout?.textContent?.trim()).toBe('تسجيل الخروج');

    const auth = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    const signOut = vi.spyOn(auth, 'signOut');
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    logout!.click();
    await settle();

    expect(signOut).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('disarms the drawer when the viewport grows back to desktop', async () => {
    await renderAt(true);
    menuButton()!.click();
    await settle();
    expect(drawer().classList.contains('sidebar--open')).toBe(true);

    matches.next(state(false));
    await settle();

    // A stale `true` would re-open the drawer on the way back down.
    expect(drawer().classList.contains('sidebar--open')).toBe(false);
    expect(menuButton()).toBeNull();
  });
});
