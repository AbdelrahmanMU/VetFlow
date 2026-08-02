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

  /**
   * The root renders the routed screen and nothing else: the shell moved into the guarded branch
   * of the route table so the login screen can render outside it (identity/ui.md S1). The shell's
   * own contract — the right-side navigation and the brand mark — is pinned in
   * `shell.component.spec.ts`, where the shell now lives. It was not dropped: that assertion
   * caught a real regression when the text brand was replaced.
   */
  it('renders the routed screen through a single outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('router-outlet')).toBeTruthy();
    expect(compiled.querySelector('.sidebar')).toBeFalsy();
  });
});
