import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { TranslationService } from '../../../core/i18n/translation.service';

/**
 * The VetFlow identity (design language §2). <b>The single place the brand is
 * rendered</b> — no screen draws the mark itself, so the identity can never
 * drift between the sidebar, the drawer and anywhere it appears next.
 *
 * The artwork lives in `/assets/branding/`, referenced rather than inlined so it
 * stays out of the JavaScript bundle (TD-107).
 *
 * <b>Not an icon.</b> §12 rules one icon family for the whole product and
 * forbids mixing families; this mark is <i>identity</i> and never stands in an
 * icon slot — not in a row, not in a button, not in an empty state.
 *
 * The accessible name is the product name from the resource file, so the mark
 * announces exactly what the text it replaced announced.
 */
@Component({
  selector: 'vf-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <img
      class="logo"
      [class.logo--mark]="variant() === 'mark'"
      [src]="source()"
      [alt]="t.t('app.name')"
      [style.block-size.px]="height()"
      decoding="async"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
    }

    .logo {
      display: block;
      inline-size: auto;
      /* The lockup's own 132×36 ratio drives the width; only the height is set. */
      max-inline-size: 100%;
    }
  `,
})
export class VfLogoComponent {
  protected readonly t = inject(TranslationService);

  /** `full` is the lockup (mark + wordmark); `mark` is the tile alone. */
  readonly variant = input<'full' | 'mark'>('full');

  /** Rendered height in px. Width follows the artwork's own ratio. */
  readonly height = input(32);

  /**
   * `logo-dark.svg` exists in the package for the dark surfaces §11 names as a
   * future capability, and has no consumer while the product is light-only.
   */
  readonly tone = input<'light' | 'dark'>('light');

  protected readonly source = computed(() => {
    if (this.variant() === 'mark') {
      return 'assets/branding/icon.svg';
    }

    return this.tone() === 'dark' ? 'assets/branding/logo-dark.svg' : 'assets/branding/logo.svg';
  });
}
