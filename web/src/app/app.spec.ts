import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the shell with the right-side navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.sidebar')).toBeTruthy();
  });

  it('brands the shell with the shared logo, keeping the product name announceable', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const logo = (fixture.nativeElement as HTMLElement).querySelector('vf-logo img');

    // The mark replaced a text brand. The accessible name must survive that swap —
    // an <img> with no alt would silently drop the product name from the a11y tree.
    expect(logo).toBeTruthy();
    expect(logo?.getAttribute('alt')).toBe('VetFlow');
    // Referenced, never inlined, so the artwork stays out of the JS bundle (TD-107).
    expect(logo?.getAttribute('src')).toContain('assets/branding/');
  });
});
